#include <filesystem>

#include <QFile>
#include <QHash>

#include "Common.h"
#include "ElementQuery.h"
#include "PackageHandler.h"
#include "Resolved.h"
#include "StyleRegistry.h"

namespace
{
QHash<QString, avu_style_s *> s_stylesById;
QHash<QString, avu_style_s *> s_stylesByPath;

int metadataString(const std::filesystem::path &package)
{
    QFile file(package / "metadata.json");
    if (!file.open(QIODevice::ReadOnly)) {
        return 0;
    }
    return avu::internString(QString::fromUtf8(file.readAll()));
}

std::filesystem::path canonicalPackagePath(const QString &path)
{
    auto result = std::filesystem::weakly_canonical(std::filesystem::path(path.toStdString()));
    if (!result.has_filename() && result.has_parent_path()) {
        result = result.parent_path();
    }
    return result;
}
}

avu_style avu::styleFromPackage(const QString &path, int *status)
{
    const auto canonical = canonicalPackagePath(path);
    const auto key = QString::fromStdString(canonical.string());
    if (auto existing = s_stylesByPath.value(key)) {
        *status = AVU_OK;
        return existing;
    }
    Union::StylePackage package{canonical};
    if (!package.isValid()) {
        *status = AVU_E_INVALID_PACKAGE;
        return nullptr;
    }
    auto style = package.load();
    if (!style || !style->load()) {
        *status = AVU_E_NO_STYLE;
        return nullptr;
    }
    auto handle = new avu_style_s{style, false};
    s_stylesByPath.insert(key, handle);
    *status = AVU_OK;
    return handle;
}

extern "C" AVU_API avu_style avu_style_default(void)
{
    AVU_GUARD_BEGIN
    if (!avu::initialized()) {
        return nullptr;
    }
    return avu::defaultStyleHandle();
    AVU_GUARD_END(nullptr)
}

extern "C" AVU_API avu_style avu_style_by_id(const uint16_t *id, int len)
{
    AVU_GUARD_BEGIN
    if (!avu::initialized()) {
        return nullptr;
    }
    const auto name = avu::fromUtf16(id, len);
    if (auto existing = s_stylesById.value(name)) {
        return existing;
    }
    auto style = Union::StyleRegistry::instance()->style(name);
    if (!style) {
        return nullptr;
    }
    auto handle = new avu_style_s{style, false};
    s_stylesById.insert(name, handle);
    return handle;
    AVU_GUARD_END(nullptr)
}

extern "C" AVU_API int avu_style_info_get(avu_style s, avu_style_info *out)
{
    AVU_GUARD_BEGIN
    if (!s || !out || !s->style) {
        return AVU_E_ARG;
    }
    out->id_string = avu::internString(s->style->id());
    out->path_string = avu::internString(QString::fromStdString(s->style->path().string()));
    out->has_errors = s->style->hasErrors() ? 1 : 0;
    out->rule_count = int32_t(s->style->rules().size());
    out->metadata_string = metadataString(s->style->path());
    return AVU_OK;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_style_from_package(const uint16_t *path, int len, avu_style *out)
{
    AVU_GUARD_BEGIN
    if (!out || !path || len <= 0) {
        return AVU_E_ARG;
    }
    *out = nullptr;
    if (!avu::initialized()) {
        return AVU_E_NOT_INIT;
    }
    int status = AVU_OK;
    *out = avu::styleFromPackage(avu::fromUtf16(path, len), &status);
    return status;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_installed_styles(avu_package_info *out, int cap)
{
    AVU_GUARD_BEGIN
    if (cap < 0 || (cap > 0 && !out)) {
        return AVU_E_ARG;
    }
    if (!avu::initialized()) {
        return AVU_E_NOT_INIT;
    }
    auto handler = Union::StyleRegistry::instance()->packageHandler();
    if (!handler) {
        return 0;
    }
    const auto packages = handler->allPackages();
    const int count = int(packages.size());
    for (int i = 0; i < count && i < cap; ++i) {
        const auto &package = packages.at(i);
        out[i].id_string = avu::internString(package.id());
        out[i].path_string = avu::internString(QString::fromStdString(package.path().string()));
        out[i].metadata_string = metadataString(package.path());
    }
    return count;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_query(avu_style s, const avu_element *chain, int count, avu_resolved *out)
{
    AVU_GUARD_BEGIN
    if (!s || !s->style || !chain || count <= 0 || !out) {
        return AVU_E_ARG;
    }
    QList<Union::Element::Ptr> elements;
    elements.reserve(count);
    for (int i = 0; i < count; ++i) {
        if (!chain[i]) {
            return AVU_E_ARG;
        }
        elements.append(chain[i]->element);
    }

    Union::ElementQuery query(s->style);
    query.setElements(elements);
    query.execute();
    if (!query.hasMatches()) {
        avu::copyResolved(nullptr, out);
        return 0;
    }
    avu::copyResolved(query.properties(), out);
    return 1;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}
