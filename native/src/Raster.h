#pragma once

#include <QImage>

#include "Common.h"

namespace avu
{
inline int copyRaster(const QImage &source, uint8_t *buf, int cap, int *outW, int *outH)
{
    const QImage image = source.convertToFormat(QImage::Format_ARGB32_Premultiplied);
    if (outW) {
        *outW = image.width();
    }
    if (outH) {
        *outH = image.height();
    }
    const int rowBytes = image.width() * 4;
    if (!buf || cap < rowBytes * image.height()) {
        return AVU_E_BUFFER;
    }
    for (int y = 0; y < image.height(); ++y) {
        std::memcpy(buf + y * rowBytes, image.constScanLine(y), size_t(rowBytes));
    }
    return 1;
}
}
