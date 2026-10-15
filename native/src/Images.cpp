#include <cmath>
#include <cstring>

#include <QIcon>
#include <QImageReader>
#include <QPainter>

#include "Raster.h"

namespace
{
QImage loadScaled(const QString &path, const QSize &target)
{
    QImageReader reader(path);
    if (reader.canRead()) {
        reader.setScaledSize(target);
        QImage image = reader.read();
        if (!image.isNull()) {
            return image;
        }
    }
    const QIcon icon(path);
    return icon.pixmap(target).toImage();
}
}

extern "C" AVU_API int avu_image_render(int path_string_id, uint32_t mask_argb, int mask_mode, int w, int h, float scale, uint8_t *buf, int cap, int *out_w, int *out_h)
{
    AVU_GUARD_BEGIN
    if (w <= 0 || h <= 0 || scale <= 0.0f) {
        return AVU_E_ARG;
    }
    const auto path = avu::stringById(path_string_id);
    if (path.isEmpty()) {
        return 0;
    }
    const QSize target(int(std::lround(w * scale)), int(std::lround(h * scale)));
    QImage image = loadScaled(path, target).convertToFormat(QImage::Format_ARGB32_Premultiplied);
    if (image.isNull()) {
        return 0;
    }
    if (mask_mode != AVU_MASK_NONE) {
        const QColor color = QColor::fromRgba(mask_argb);
        QImage masked(image.size(), QImage::Format_ARGB32_Premultiplied);
        for (int y = 0; y < image.height(); ++y) {
            auto src = reinterpret_cast<const QRgb *>(image.constScanLine(y));
            auto dst = reinterpret_cast<QRgb *>(masked.scanLine(y));
            for (int x = 0; x < image.width(); ++x) {
                int a = qAlpha(src[x]);
                if (mask_mode == AVU_MASK_INVERTED) {
                    a = 255 - a;
                }
                const int alpha = color.alpha() * a / 255;
                dst[x] = qPremultiply(qRgba(color.red(), color.green(), color.blue(), alpha));
            }
        }
        image = masked;
    }
    return avu::copyRaster(image, buf, cap, out_w, out_h);
    AVU_GUARD_END(AVU_E_EXCEPTION)
}
