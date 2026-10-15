#include <filesystem>
#include <optional>

#include <QBuffer>
#include <QCoreApplication>
#include <QDataStream>
#include <QDir>
#include <QEvent>
#include <QFile>
#include <QFileInfo>
#include <QJsonArray>
#include <QJsonDocument>
#include <QJsonObject>
#include <QRegularExpression>
#include <QSet>
#include <QStandardPaths>
#include <QXmlStreamReader>

#include "Color.h"
#include "Common.h"
#include "ElementQuery.h"
#include "Leaves.g.h"
#include "PackageHandler.h"
#include "Selector.h"
#include "StyleRegistry.h"
#include "StyleRule.h"

namespace
{
constexpr auto StreamVersion = QDataStream::Qt_6_9;

QString hexArgb(uint32_t argb)
{
    return QStringLiteral("#%1").arg(argb, 8, 16, QLatin1Char('0'));
}

QJsonObject readColor(QDataStream &stream)
{
    qint32 type = 0;
    stream >> type;
    QJsonObject result;
    switch (type) {
    case 0:
        result[u"type"] = QStringLiteral("empty");
        break;
    case 1: {
        quint8 r, g, b, a;
        stream >> r >> g >> b >> a;
        result[u"type"] = QStringLiteral("rgba");
        result[u"r"] = int(r);
        result[u"g"] = int(g);
        result[u"b"] = int(b);
        result[u"a"] = int(a);
        break;
    }
    case 2: {
        QString source;
        QStringList arguments;
        stream >> source >> arguments;
        result[u"type"] = QStringLiteral("custom");
        result[u"source"] = source;
        result[u"args"] = QJsonArray::fromStringList(arguments);
        break;
    }
    case 3:
    case 4:
    case 5: {
        static const char *const names[] = {"add", "subtract", "multiply"};
        result[u"type"] = QString::fromLatin1(names[type - 3]);
        result[u"a"] = readColor(stream);
        result[u"b"] = readColor(stream);
        break;
    }
    case 6: {
        result[u"type"] = QStringLiteral("set");
        result[u"color"] = readColor(stream);
        for (auto channel : {u"r", u"g", u"b", u"a"}) {
            bool has = false;
            stream >> has;
            if (has) {
                quint8 v;
                stream >> v;
                result[QString(channel)] = int(v);
            } else {
                result[QString(channel)] = QJsonValue::Null;
            }
        }
        break;
    }
    case 7: {
        result[u"type"] = QStringLiteral("mix");
        result[u"a"] = readColor(stream);
        result[u"b"] = readColor(stream);
        double amount = 0.0;
        stream >> amount;
        result[u"amount"] = amount;
        break;
    }
    default:
        result[u"type"] = QStringLiteral("unknown");
        break;
    }
    return result;
}

QJsonObject symbolicColor(const Union::Color &color)
{
    QByteArray data;
    {
        QDataStream out(&data, QIODevice::WriteOnly);
        out.setVersion(StreamVersion);
        out << color;
    }
    QDataStream in(data);
    in.setVersion(StreamVersion);
    return readColor(in);
}

QJsonObject selectorToJson(const Union::Selector &selector)
{
    QByteArray data;
    {
        QDataStream out(&data, QIODevice::WriteOnly);
        out.setVersion(StreamVersion);
        selector.writeToDataStream(out);
    }
    QDataStream in(data);
    in.setVersion(StreamVersion);
    qint32 type = 0;
    in >> type;

    static const char *const kinds[] = {"Empty",
                                        "Type",
                                        "Id",
                                        "State",
                                        "Hint",
                                        "AttributeExists",
                                        "AttributeEquals",
                                        "AttributeSubstringMatch",
                                        "AnyElement",
                                        "ChildCombinator",
                                        "DescendantCombinator"};
    QJsonObject result;
    result[u"kind"] = (type >= 0 && type <= 10) ? QString::fromLatin1(kinds[type]) : QStringLiteral("Unknown");
    switch (Union::SelectorType(type)) {
    case Union::SelectorType::Type:
    case Union::SelectorType::Id:
    case Union::SelectorType::Hint:
    case Union::SelectorType::AttributeExists: {
        QString value;
        in >> value;
        result[u"value"] = value;
        break;
    }
    case Union::SelectorType::State: {
        qint32 state = 0;
        in >> state;
        result[u"state"] = state;
        break;
    }
    case Union::SelectorType::AttributeEquals: {
        QString name;
        QVariant value;
        in >> name >> value;
        result[u"name"] = name;
        if (value.typeId() == QMetaType::QString) {
            result[u"valueType"] = QStringLiteral("string");
            result[u"value"] = value.toString();
        } else if (value.canConvert<double>()) {
            result[u"valueType"] = QStringLiteral("number");
            result[u"value"] = value.toDouble();
        } else {
            result[u"valueType"] = QStringLiteral("string");
            result[u"value"] = value.toString();
        }
        break;
    }
    case Union::SelectorType::AttributeSubstringMatch: {
        QString name;
        QString value;
        in >> name >> value;
        result[u"name"] = name;
        result[u"value"] = value;
        break;
    }
    default:
        break;
    }
    result[u"weight"] = selector.weight();
    return result;
}

QString relativeToPackage(const std::filesystem::path &path, const std::filesystem::path &package)
{
    std::error_code error;
    auto relative = std::filesystem::relative(path, package, error);
    if (error || relative.empty() || relative.string().starts_with("..")) {
        return QString::fromStdString(path.string());
    }
    return QString::fromStdString(relative.generic_string());
}

QJsonObject fontToJson(const QFont &font)
{
    int32_t family = 0, weight = 0, italic = 0, mask = 0;
    float point = 0.0f, pixel = 0.0f;
    avu::copyFont(font, &family, &point, &pixel, &weight, &italic, &mask);
    QJsonObject result;
    if (mask & AVU_FONT_FAMILY) {
        result[u"family"] = avu::stringById(family);
    }
    if (mask & AVU_FONT_POINT_SIZE) {
        result[u"pointSize"] = double(point);
    }
    if (mask & AVU_FONT_PIXEL_SIZE) {
        result[u"pixelSize"] = double(pixel);
    }
    if (mask & AVU_FONT_WEIGHT) {
        result[u"weight"] = weight;
    }
    if (mask & AVU_FONT_ITALIC) {
        result[u"italic"] = italic != 0;
    }
    return result;
}

struct LeafWriter {
    QJsonObject *target;
    std::filesystem::path package;
    bool resolveColors;

    void put(const char *name, const QJsonValue &value)
    {
        target->insert(QString::fromLatin1(name), value);
    }

    void operator()(int, const char *name, qreal value)
    {
        put(name, double(value));
    }
    void operator()(int, const char *name, int value)
    {
        put(name, value);
    }
    void operator()(int, const char *name, bool value)
    {
        put(name, value);
    }
    template<typename E>
        requires std::is_enum_v<E>
    void operator()(int, const char *name, E value)
    {
        put(name, int(value));
    }
    template<typename E>
    void operator()(int, const char *name, QFlags<E> value)
    {
        put(name, int(value.toInt()));
    }
    void operator()(int, const char *name, const Union::Color &value)
    {
        if (resolveColors) {
            put(name, hexArgb(avu::colorToArgb(value)));
        } else {
            put(name, symbolicColor(value));
        }
    }
    void operator()(int, const char *name, const QString &value)
    {
        put(name, value);
    }
    void operator()(int, const char *name, const QUrl &value)
    {
        put(name, value.toString());
    }
    void operator()(int, const char *name, const std::filesystem::path &value)
    {
        put(name, relativeToPackage(value, package));
    }
    void operator()(int, const char *name, const QFont &value)
    {
        put(name, fontToJson(value));
    }
};

bool isInside(const std::filesystem::path &path, const std::filesystem::path &package)
{
    std::error_code error;
    const auto canonical = std::filesystem::weakly_canonical(path, error);
    if (error) {
        return false;
    }
    const auto relative = canonical.lexically_relative(package);
    return !relative.empty() && !relative.string().starts_with("..");
}

struct OutsideCollector {
    QSet<QString> *outside;
    std::filesystem::path package;

    template<typename T>
    void operator()(int, const char *, const T &)
    {
    }
    void operator()(int, const char *, const QUrl &value)
    {
        if (value.isLocalFile()) {
            check(std::filesystem::path(value.toLocalFile().toStdString()));
        }
    }
    void operator()(int, const char *, const std::filesystem::path &value)
    {
        check(value);
    }
    void check(const std::filesystem::path &value)
    {
        if (!value.empty() && !isInside(value, package)) {
            outside->insert(relativeToPackage(value, package.parent_path()));
        }
    }
};

const QRegularExpression &cssComments()
{
    static const QRegularExpression comments(QStringLiteral(R"(/\*.*?\*/)"), QRegularExpression::DotMatchesEverythingOption);
    return comments;
}

QString cssText(const std::filesystem::path &file)
{
    QFile input(file);
    if (!input.open(QIODevice::ReadOnly)) {
        return {};
    }
    auto text = QString::fromUtf8(input.readAll());
    text.remove(cssComments());
    return text;
}

QSet<QString> variableNames(const std::filesystem::path &path)
{
    static const QRegularExpression declarations(QStringLiteral(R"((--[A-Za-z0-9_-]+)\s*:)"));
    QSet<QString> names;
    auto scan = [&](const std::filesystem::path &file) {
        for (const auto &match : declarations.globalMatch(cssText(file))) {
            names.insert(match.captured(1));
        }
    };
    std::error_code error;
    if (std::filesystem::is_regular_file(path, error)) {
        scan(path);
        return names;
    }
    if (!std::filesystem::is_directory(path, error)) {
        return names;
    }
    for (const auto &entry : std::filesystem::recursive_directory_iterator(path, error)) {
        if (entry.is_regular_file() && entry.path().extension() == ".css") {
            scan(entry.path());
        }
    }
    return names;
}

std::optional<QJsonArray> sharedVariables(const std::filesystem::path &package)
{
    auto handler = Union::StyleRegistry::instance()->packageHandler();
    if (!handler) {
        return QJsonArray{};
    }
    QList<std::pair<QString, std::filesystem::path>> others;
    for (const auto &installed : handler->allPackages(Union::PackageHandler::OperationFlag::IncludeHidden)) {
        std::error_code error;
        const auto path = std::filesystem::weakly_canonical(installed.path(), error);
        if (path == package) {
            return std::nullopt;
        }
        others.append({installed.id(), installed.path() / "contents" / "css"});
    }
    const auto defaults = QStandardPaths::locate(QStandardPaths::GenericDataLocation, QStringLiteral("union/css/defaults/default.css"));
    if (!defaults.isEmpty()) {
        others.append({QStringLiteral("default.css"), std::filesystem::path(defaults.toStdString())});
    }

    const auto own = variableNames(package / "contents" / "css");
    QMap<QString, QStringList> shared;
    for (const auto &[name, path] : others) {
        for (const auto &variable : variableNames(path)) {
            if (own.contains(variable)) {
                shared[variable].append(name);
            }
        }
    }

    QJsonArray result;
    for (auto itr = shared.begin(); itr != shared.end(); ++itr) {
        auto styles = itr.value();
        styles.sort();
        styles.removeDuplicates();
        QJsonObject entry;
        entry[u"name"] = itr.key();
        entry[u"styles"] = QJsonArray::fromStringList(styles);
        result.append(entry);
    }
    return result;
}

void collectImports(const std::filesystem::path &package, QSet<QString> *outside)
{
    static const QRegularExpression imports(QStringLiteral(R"(@import\s+(?:url\(\s*)?["']?([^"')\s;]+))"));
    const auto css = package / "contents" / "css";
    std::error_code error;
    if (!std::filesystem::is_directory(css, error)) {
        return;
    }
    for (const auto &entry : std::filesystem::recursive_directory_iterator(css, error)) {
        if (!entry.is_regular_file() || entry.path().extension() != ".css") {
            continue;
        }
        for (const auto &match : imports.globalMatch(cssText(entry.path()))) {
            const auto target = std::filesystem::path(match.captured(1).toStdString());
            const auto resolved = target.is_absolute() ? target : entry.path().parent_path() / target;
            if (!isInside(resolved, package)) {
                outside->insert(relativeToPackage(std::filesystem::weakly_canonical(resolved, error), package.parent_path()));
            }
        }
    }
}

QJsonObject readMetadata(const std::filesystem::path &package)
{
    QFile file(package / "metadata.json");
    if (!file.open(QIODevice::ReadOnly)) {
        return {};
    }
    return QJsonDocument::fromJson(file.readAll()).object();
}

QMap<QString, QString> styleAttributes(const QXmlStreamReader &reader)
{
    QMap<QString, QString> values;
    for (const auto &attribute : reader.attributes()) {
        values.insert(attribute.name().toString(), attribute.value().toString());
    }
    const auto style = values.value(QStringLiteral("style"));
    for (const auto &part : style.split(u';', Qt::SkipEmptyParts)) {
        const auto colon = part.indexOf(u':');
        if (colon > 0) {
            values.insert(part.left(colon).trimmed(), part.mid(colon + 1).trimmed());
        }
    }
    return values;
}

QJsonObject imageToJson(const QString &file)
{
    QFile input(file);
    QJsonObject result;
    if (!input.open(QIODevice::ReadOnly)) {
        return result;
    }
    QXmlStreamReader reader(&input);
    QJsonArray shapes;
    while (!reader.atEnd()) {
        reader.readNext();
        if (!reader.isStartElement()) {
            continue;
        }
        const auto name = reader.name().toString();
        const auto values = styleAttributes(reader);
        if (name == u"svg") {
            QJsonArray box;
            for (const auto &part : values.value(QStringLiteral("viewBox")).split(u' ', Qt::SkipEmptyParts)) {
                box.append(part.toDouble());
            }
            result[u"viewBox"] = box;
            continue;
        }
        if (name != u"path" && name != u"circle" && name != u"rect") {
            continue;
        }
        QJsonObject shape;
        shape[u"kind"] = name;
        for (auto key : {"d", "cx", "cy", "r", "x", "y", "width", "height"}) {
            const auto k = QString::fromLatin1(key);
            if (values.contains(k)) {
                shape[k] = (k == u"d") ? QJsonValue(values.value(k)) : QJsonValue(values.value(k).toDouble());
            }
        }
        const auto stroke = values.value(QStringLiteral("stroke"));
        const auto fill = values.value(QStringLiteral("fill"));
        shape[u"stroke"] = !stroke.isEmpty() && stroke != u"none";
        shape[u"fill"] = !fill.isEmpty() && fill != u"none";
        if (values.contains(QStringLiteral("stroke-width"))) {
            shape[u"strokeWidth"] = values.value(QStringLiteral("stroke-width")).toDouble();
        }
        shape[u"lineCap"] = values.value(QStringLiteral("stroke-linecap"), QStringLiteral("butt"));
        shape[u"lineJoin"] = values.value(QStringLiteral("stroke-linejoin"), QStringLiteral("miter"));
        shapes.append(shape);
    }
    result[u"shapes"] = shapes;
    return result;
}

bool writeJson(const QString &path, const QJsonObject &object)
{
    QFile file(path);
    if (!file.open(QIODevice::WriteOnly | QIODevice::Truncate)) {
        return false;
    }
    file.write(QJsonDocument(object).toJson(QJsonDocument::Indented));
    return true;
}

uint32_t stateBit(const QString &name)
{
    const auto n = name.toLower();
    if (n == u"hovered") {
        return 1u << 0;
    }
    if (n == u"activefocus" || n == u"active-focus") {
        return 1u << 1;
    }
    if (n == u"visualfocus" || n == u"visual-focus") {
        return 1u << 2;
    }
    if (n == u"pressed") {
        return 1u << 3;
    }
    if (n == u"checked") {
        return 1u << 4;
    }
    if (n == u"disabled") {
        return 1u << 5;
    }
    if (n == u"highlighted") {
        return 1u << 6;
    }
    return 0;
}

void sendPaletteChange()
{
    QEvent event(QEvent::ApplicationPaletteChange);
    QCoreApplication::sendEvent(qApp, &event);
}
}

int avu::exportStyle(const std::shared_ptr<Union::Style> &style, const QString &outPath)
{
    QJsonObject root;
    root[u"format"] = 1;
    root[u"union"] = QStringLiteral(AVU_UNION_VERSION);
    root[u"schema"] = QStringLiteral(AVU_UNION_SCHEMA);
    root[u"style"] = style->id();
    root[u"hasErrors"] = style->hasErrors();

    const auto package = style->path();
    root[u"metadata"] = readMetadata(package);

    QSet<QString> outside;
    const auto canonicalPackage = std::filesystem::weakly_canonical(package);
    for (const auto &rule : style->rules()) {
        avu::visitLeaves(rule->properties(), OutsideCollector{&outside, canonicalPackage});
    }
    collectImports(canonicalPackage, &outside);
    auto outsideList = outside.values();
    outsideList.sort();
    root[u"outside"] = QJsonArray::fromStringList(outsideList);
    if (auto shared = sharedVariables(canonicalPackage)) {
        root[u"sharedVariables"] = *shared;
    }

    QJsonArray rules;
    for (const auto &rule : style->rules()) {
        QJsonObject entry;
        QJsonArray selectors;
        for (const auto &selector : rule->selectors()) {
            selectors.append(selectorToJson(selector));
        }
        entry[u"selectors"] = selectors;
        entry[u"weight"] = rule->selectors().weight();
        QJsonObject properties;
        avu::visitLeaves(rule->properties(), LeafWriter{&properties, package.parent_path(), false});
        entry[u"properties"] = properties;
        rules.append(entry);
    }
    root[u"rules"] = rules;

    QJsonObject images;
    const auto imageDir = QString::fromStdString((package / "contents" / "images").string());
    for (const auto &info : QDir(imageDir).entryInfoList({QStringLiteral("*.svg")}, QDir::Files, QDir::Name)) {
        images[style->id() + QStringLiteral("/contents/images/") + info.fileName()] = imageToJson(info.absoluteFilePath());
    }
    root[u"images"] = images;

    return writeJson(outPath, root) ? AVU_OK : AVU_E_IO;
}

int avu::exportScheme(const QString &colorsFile, const QString &outPath)
{
    const auto previous = qApp->property("KDE_COLOR_SCHEME_PATH");
    qApp->setProperty("KDE_COLOR_SCHEME_PATH", QFileInfo(colorsFile).absoluteFilePath());
    sendPaletteChange();

    QJsonObject root;
    root[u"format"] = 1;
    root[u"name"] = QFileInfo(colorsFile).completeBaseName();
    QJsonArray colors;
    for (int group = 0; group < 3; ++group) {
        for (int set = 0; set < 7; ++set) {
            for (int kind = 0; kind < 3; ++kind) {
                const int roles = kind == 2 ? 4 : 8;
                for (int role = 0; role < roles; ++role) {
                    uint32_t argb = 0;
                    avu_scheme_color(group, set, kind, role, &argb);
                    QJsonObject entry;
                    entry[u"key"] = schemeColorArgument(-3, group) + u'.' + schemeColorArgument(-2, set) + u'.' + schemeColorArgument(-1, kind) + u'.'
                        + schemeColorArgument(kind, role);
                    entry[u"argb"] = hexArgb(argb);
                    colors.append(entry);
                }
            }
        }
    }
    root[u"colors"] = colors;

    qApp->setProperty("KDE_COLOR_SCHEME_PATH", previous);
    sendPaletteChange();

    return writeJson(outPath, root) ? AVU_OK : AVU_E_IO;
}

namespace
{
QList<Union::Element::Ptr> chainFromJson(const QJsonArray &parts)
{
    QList<Union::Element::Ptr> elements;
    for (const auto &part : parts) {
        const auto p = part.toObject();
        auto element = Union::Element::create();
        element->setType(p.value(u"type").toString());
        element->setId(p.value(u"id").toString());
        uint32_t states = 0;
        for (const auto &s : p.value(u"states").toArray()) {
            states |= stateBit(s.toString());
        }
        element->setStates(Union::Element::States::fromInt(int(states)));
        QStringList hints;
        for (const auto &h : p.value(u"hints").toArray()) {
            hints.append(h.toString());
        }
        element->setHints(hints);
        QVariantMap attributes;
        const auto attrs = p.value(u"attributes").toObject();
        for (auto itr = attrs.begin(); itr != attrs.end(); ++itr) {
            attributes.insert(itr.key(), itr.value().isDouble() ? QVariant(itr.value().toDouble()) : QVariant(itr.value().toString()));
        }
        element->setAttributes(attributes);
        elements.append(element);
    }
    return elements;
}

QJsonArray readChains(const QString &path, bool *ok)
{
    QFile input(path);
    if (!input.open(QIODevice::ReadOnly)) {
        *ok = false;
        return {};
    }
    *ok = true;
    const auto corpus = QJsonDocument::fromJson(input.readAll());
    return corpus.isArray() ? corpus.array() : corpus.object().value(u"chains").toArray();
}

QJsonObject queryLeaves(const std::shared_ptr<Union::Style> &style, const QJsonArray &parts, bool *matched)
{
    Union::ElementQuery query(style);
    query.setElements(chainFromJson(parts));
    query.execute();
    *matched = query.hasMatches();
    QJsonObject leaves;
    if (query.hasMatches()) {
        avu::visitLeaves(query.properties(), LeafWriter{&leaves, style->path().parent_path(), true});
    }
    return leaves;
}
}

int avu::exportQuery(const std::shared_ptr<Union::Style> &style, const QString &chainsPath, const QString &outPath)
{
    bool ok = false;
    const auto chains = readChains(chainsPath, &ok);
    if (!ok) {
        return AVU_E_IO;
    }

    QJsonArray results;
    for (const auto &value : chains) {
        const auto entry = value.toObject();
        QJsonObject result;
        result[u"id"] = entry.value(u"id");
        bool matched = false;
        result[u"leaves"] = queryLeaves(style, entry.value(u"chain").toArray(), &matched);
        result[u"matched"] = matched;
        results.append(result);
    }

    QJsonObject root;
    root[u"format"] = 1;
    root[u"union"] = QStringLiteral(AVU_UNION_VERSION);
    root[u"style"] = style->id();
    root[u"results"] = results;
    return writeJson(outPath, root) ? AVU_OK : AVU_E_IO;
}

int avu::exportParity(const QList<std::shared_ptr<Union::Style>> &styles, const QStringList &colorFiles, const QString &chainsPath, const QString &outPath)
{
    bool ok = false;
    const auto chains = readChains(chainsPath, &ok);
    if (!ok) {
        return AVU_E_IO;
    }

    QJsonArray ids;
    for (const auto &value : chains) {
        ids.append(value.toObject().value(u"id"));
    }

    QJsonArray sets;
    QHash<QByteArray, int> setIndex;
    QJsonArray runs;
    const auto previous = qApp->property("KDE_COLOR_SCHEME_PATH");
    for (const auto &colors : colorFiles) {
        qApp->setProperty("KDE_COLOR_SCHEME_PATH", QFileInfo(colors).absoluteFilePath());
        sendPaletteChange();
        for (const auto &style : styles) {
            QJsonArray results;
            for (const auto &value : chains) {
                bool matched = false;
                const auto leaves = queryLeaves(style, value.toObject().value(u"chain").toArray(), &matched);
                const auto key = QJsonDocument(leaves).toJson(QJsonDocument::Compact);
                auto itr = setIndex.constFind(key);
                int index;
                if (itr == setIndex.cend()) {
                    index = int(sets.size());
                    sets.append(leaves);
                    setIndex.insert(key, index);
                } else {
                    index = itr.value();
                }
                results.append(QJsonArray{matched ? 1 : 0, index});
            }
            QJsonObject run;
            run[u"style"] = style->id();
            run[u"scheme"] = QFileInfo(colors).completeBaseName();
            run[u"results"] = results;
            runs.append(run);
        }
    }
    qApp->setProperty("KDE_COLOR_SCHEME_PATH", previous);
    sendPaletteChange();

    QJsonObject root;
    root[u"format"] = 1;
    root[u"union"] = QStringLiteral(AVU_UNION_VERSION);
    root[u"ids"] = ids;
    root[u"runs"] = runs;
    root[u"sets"] = sets;

    QFile file(outPath);
    if (!file.open(QIODevice::WriteOnly | QIODevice::Truncate)) {
        return AVU_E_IO;
    }
    auto text = QJsonDocument(root).toJson(QJsonDocument::Compact);
    text.replace("},{", "},\n{");
    text.replace("],[", "],\n[");
    file.write(text);
    file.write("\n");
    return AVU_OK;
}

extern "C" AVU_API int avu_export_style(avu_style s, const uint16_t *out_path, int len)
{
    AVU_GUARD_BEGIN
    if (!s || !s->style) {
        return AVU_E_ARG;
    }
    return avu::exportStyle(s->style, avu::fromUtf16(out_path, len));
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_export_scheme(const uint16_t *colors_file, int len, const uint16_t *out_path, int olen)
{
    AVU_GUARD_BEGIN
    if (!avu::initialized()) {
        return AVU_E_NOT_INIT;
    }
    return avu::exportScheme(avu::fromUtf16(colors_file, len), avu::fromUtf16(out_path, olen));
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_export_query(avu_style s, const uint16_t *chains_path, int len, const uint16_t *out_path, int olen)
{
    AVU_GUARD_BEGIN
    if (!s || !s->style) {
        return AVU_E_ARG;
    }
    return avu::exportQuery(s->style, avu::fromUtf16(chains_path, len), avu::fromUtf16(out_path, olen));
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_export_parity(const uint16_t *styles, int slen, const uint16_t *colors, int clen, const uint16_t *chains_path, int len, const uint16_t *out_path, int olen)
{
    AVU_GUARD_BEGIN
    if (!avu::initialized()) {
        return AVU_E_NOT_INIT;
    }
    QList<std::shared_ptr<Union::Style>> resolved;
    for (const auto &id : avu::fromUtf16(styles, slen).split(u'\n', Qt::SkipEmptyParts)) {
        auto style = Union::StyleRegistry::instance()->style(id);
        if (!style) {
            return AVU_E_NO_STYLE;
        }
        resolved.append(style);
    }
    const auto colorFiles = avu::fromUtf16(colors, clen).split(u'\n', Qt::SkipEmptyParts);
    return avu::exportParity(resolved, colorFiles, avu::fromUtf16(chains_path, len), avu::fromUtf16(out_path, olen));
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_export_parity_handles(const avu_style *styles, int count, const uint16_t *colors, int clen, const uint16_t *chains_path, int len, const uint16_t *out_path, int olen)
{
    AVU_GUARD_BEGIN
    if (!avu::initialized()) {
        return AVU_E_NOT_INIT;
    }
    if (count < 0 || (count > 0 && !styles)) {
        return AVU_E_ARG;
    }
    QList<std::shared_ptr<Union::Style>> resolved;
    for (int i = 0; i < count; ++i) {
        if (!styles[i] || !styles[i]->style) {
            return AVU_E_ARG;
        }
        resolved.append(styles[i]->style);
    }
    const auto colorFiles = avu::fromUtf16(colors, clen).split(u'\n', Qt::SkipEmptyParts);
    return avu::exportParity(resolved, colorFiles, avu::fromUtf16(chains_path, len), avu::fromUtf16(out_path, olen));
    AVU_GUARD_END(AVU_E_EXCEPTION)
}
