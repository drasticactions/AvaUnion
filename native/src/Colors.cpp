#include <QCoreApplication>
#include <QEvent>
#include <QFileInfo>
#include <QStandardPaths>

#include "Color.h"
#include "Common.h"

namespace
{
const char *const Groups[] = {"active", "inactive", "disabled"};
const char *const Sets[] = {"view", "window", "button", "selection", "tooltip", "complementary", "header"};
const char *const Kinds[] = {"background", "foreground", "decoration"};
const char *const BackgroundRoles[] = {"normal", "alternate", "active", "link", "visited", "negative", "neutral", "positive"};
const char *const ForegroundRoles[] = {"text", "inactive", "active", "link", "visited", "negative", "neutral", "positive"};
const char *const DecorationRoles[] = {"focus", "hover", "frame", "framecontrast"};

template<size_t N>
QString pick(const char *const (&values)[N], int index)
{
    if (index < 0 || size_t(index) >= N) {
        return QString{};
    }
    return QString::fromLatin1(values[index]);
}
}

QString avu::schemeColorArgument(int kind, int index)
{
    switch (kind) {
    case -3:
        return pick(Groups, index);
    case -2:
        return pick(Sets, index);
    case -1:
        return pick(Kinds, index);
    case 0:
        return pick(BackgroundRoles, index);
    case 1:
        return pick(ForegroundRoles, index);
    case 2:
        return pick(DecorationRoles, index);
    }
    return QString{};
}

extern "C" AVU_API int avu_scheme_color(int group, int set, int role_kind, int role, uint32_t *argb)
{
    AVU_GUARD_BEGIN
    if (!argb || !avu::initialized()) {
        return AVU_E_ARG;
    }
    const QStringList arguments{
        avu::schemeColorArgument(-3, group),
        avu::schemeColorArgument(-2, set),
        avu::schemeColorArgument(-1, role_kind),
        avu::schemeColorArgument(role_kind, role),
    };
    for (const auto &argument : arguments) {
        if (argument.isEmpty()) {
            return AVU_E_ARG;
        }
    }
    *argb = avu::colorToArgb(Union::Color::custom(QStringLiteral("kcolorscheme"), arguments));
    return AVU_OK;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_set_color_scheme_path(const uint16_t *path, int len)
{
    AVU_GUARD_BEGIN
    if (!avu::initialized()) {
        return AVU_E_NOT_INIT;
    }
    auto value = avu::fromUtf16(path, len);
    if (!value.isEmpty() && !value.contains(u'/')) {
        auto located = QStandardPaths::locate(QStandardPaths::GenericDataLocation, QStringLiteral("color-schemes/") + value + QStringLiteral(".colors"));
        if (located.isEmpty()) {
            return AVU_E_IO;
        }
        value = located;
    } else if (!value.isEmpty() && !QFileInfo::exists(value)) {
        return AVU_E_IO;
    }
    qApp->setProperty("KDE_COLOR_SCHEME_PATH", value.isEmpty() ? QVariant{} : QVariant(value));
    QEvent event(QEvent::ApplicationPaletteChange);
    QCoreApplication::sendEvent(qApp, &event);
    return AVU_OK;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}
