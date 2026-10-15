#include <cmath>
#include <cstring>

#include <QIcon>

#include <KIconColors>
#include <KIconLoader>

#include "PlatformPlugin.h"
#include "Raster.h"
#include "StyleRegistry.h"

extern "C" AVU_API int avu_icon_render(const uint16_t *name, int len, uint32_t argb_or_zero, int w, int h, float scale, uint8_t *buf, int cap, int *out_w, int *out_h)
{
    AVU_GUARD_BEGIN
    if (!avu::initialized() || w <= 0 || h <= 0 || scale <= 0.0f) {
        return AVU_E_ARG;
    }
    auto platform = Union::StyleRegistry::instance()->platform();
    if (!platform) {
        return 0;
    }
    const QColor color = argb_or_zero == 0 ? QColor(Qt::transparent) : QColor::fromRgba(argb_or_zero);
    const auto iconName = avu::fromUtf16(name, len);
    QIcon icon;
    if (color != Qt::transparent) {
        KIconColors colors;
        colors.setText(color);
        icon = KDE::icon(iconName, colors);
    } else {
        icon = platform->platformIcon(iconName, color);
    }
    if (icon.isNull()) {
        return 0;
    }
    const QPixmap pixmap = icon.pixmap(QSize(w, h), qreal(scale));
    if (pixmap.isNull()) {
        return 0;
    }
    QImage image = pixmap.toImage();
    const QSize target(int(std::lround(w * scale)), int(std::lround(h * scale)));
    if (image.size() != target) {
        image = image.scaled(target, Qt::IgnoreAspectRatio, Qt::SmoothTransformation);
    }
    return avu::copyRaster(image, buf, cap, out_w, out_h);
    AVU_GUARD_END(AVU_E_EXCEPTION)
}
