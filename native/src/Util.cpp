#include <QHash>
#include <QList>

#include "Color.h"
#include "Common.h"

namespace
{
struct StringTable {
    QHash<QString, int> ids;
    QList<QString> values{QString{}};
};

StringTable &table()
{
    static StringTable t;
    return t;
}
}

int avu::internString(const QString &value)
{
    if (value.isEmpty()) {
        return 0;
    }
    auto &t = table();
    if (auto itr = t.ids.constFind(value); itr != t.ids.cend()) {
        return itr.value();
    }
    const int id = int(t.values.size());
    t.values.append(value);
    t.ids.insert(value, id);
    return id;
}

QString avu::stringById(int id)
{
    auto &t = table();
    if (id < 0 || id >= t.values.size()) {
        return QString{};
    }
    return t.values.at(id);
}

uint32_t avu::qcolorToArgb(const QColor &c)
{
    return (uint32_t(c.alpha()) << 24) | (uint32_t(c.red()) << 16) | (uint32_t(c.green()) << 8) | uint32_t(c.blue());
}

uint32_t avu::colorToArgb(const Union::Color &color)
{
    return qcolorToArgb(color.toQColor());
}

void avu::copyFont(const QFont &font, int32_t *family, float *pointSize, float *pixelSize, int32_t *weight, int32_t *italic, int32_t *mask)
{
    const auto resolved = font.resolveMask();
    int32_t m = 0;
    if (resolved & QFont::FamilyResolved) {
        *family = internString(font.family());
        m |= AVU_FONT_FAMILY;
    }
    if (resolved & QFont::SizeResolved) {
        if (font.pixelSize() > 0) {
            *pixelSize = float(font.pixelSize());
            m |= AVU_FONT_PIXEL_SIZE;
        } else {
            *pointSize = float(font.pointSizeF());
            m |= AVU_FONT_POINT_SIZE;
        }
    }
    if (resolved & QFont::WeightResolved) {
        *weight = int32_t(font.weight());
        m |= AVU_FONT_WEIGHT;
    }
    if (resolved & QFont::StyleResolved) {
        *italic = font.italic() ? 1 : 0;
        m |= AVU_FONT_ITALIC;
    }
    *mask = m;
}

extern "C" AVU_API int avu_string(int id, uint16_t *buf, int cap)
{
    AVU_GUARD_BEGIN
    const auto value = avu::stringById(id);
    if (buf && cap > 0) {
        const int n = std::min(int(value.size()), cap);
        std::copy_n(reinterpret_cast<const uint16_t *>(value.utf16()), n, buf);
    }
    return int(value.size());
    AVU_GUARD_END(AVU_E_EXCEPTION)
}
