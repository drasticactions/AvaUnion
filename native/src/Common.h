#pragma once

#include <memory>

#include <QColor>
#include <QFont>
#include <QString>
#include <QStringList>

#include "Element.h"
#include "Style.h"

#include "avaunion.h"

struct avu_style_s {
    std::shared_ptr<Union::Style> style;
    bool followsDefault = false;
};

struct avu_element_s {
    Union::Element::Ptr element;
};

namespace avu
{
int internString(const QString &value);
QString stringById(int id);

inline QString fromUtf16(const uint16_t *s, int len)
{
    if (!s || len <= 0) {
        return QString{};
    }
    return QString(reinterpret_cast<const QChar *>(s), len);
}

uint32_t colorToArgb(const class Union::Color &color);
uint32_t qcolorToArgb(const QColor &color);

void copyFont(const QFont &font, int32_t *family, float *pointSize, float *pixelSize, int32_t *weight, int32_t *italic, int32_t *mask);

bool initialized();
avu_style defaultStyleHandle();
void refreshDefaultStyle();
avu_style styleFromPackage(const QString &path, int *status);

void notifyChange(uint32_t flags);
void notifyLog(const QString &message);
void installEventWatchers(bool platformThemeLoaded);

int exportStyle(const std::shared_ptr<Union::Style> &style, const QString &outPath);
int exportScheme(const QString &colorsFile, const QString &outPath);
int exportQuery(const std::shared_ptr<Union::Style> &style, const QString &chainsPath, const QString &outPath);
int exportParity(const QList<std::shared_ptr<Union::Style>> &styles, const QStringList &colorFiles, const QString &chainsPath, const QString &outPath);

QString schemeColorArgument(int kind, int index);
}

#define AVU_GUARD_BEGIN try {
#define AVU_GUARD_END(errorValue)                                                                                                                              \
    }                                                                                                                                                          \
    catch (...)                                                                                                                                                \
    {                                                                                                                                                          \
        return errorValue;                                                                                                                                     \
    }
#define AVU_GUARD_END_VOID                                                                                                                                     \
    }                                                                                                                                                          \
    catch (...)                                                                                                                                                \
    {                                                                                                                                                          \
    }
