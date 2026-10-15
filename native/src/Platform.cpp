#include <cstring>

#include <QFontDatabase>
#include <QGuiApplication>
#include <QIcon>

#include <KConfigGroup>
#include <KIconLoader>
#include <KIconTheme>
#include <KSharedConfig>

#include "Common.h"
#include "PlatformPlugin.h"
#include "StyleRegistry.h"

namespace
{
QFont kdeglobalsFont(const char *key)
{
    auto config = KSharedConfig::openConfig(QStringLiteral("kdeglobals"));
    config->reparseConfiguration();
    const auto group = KConfigGroup(config, QStringLiteral("General"));
    const auto value = group.readEntry(key, QString{});
    QFont font = QGuiApplication::font();
    if (!value.isEmpty()) {
        font.fromString(value);
    }
    return font;
}
}

extern "C" AVU_API int avu_platform(avu_platform_info *out)
{
    AVU_GUARD_BEGIN
    if (!out || !avu::initialized()) {
        return AVU_E_ARG;
    }
    std::memset(out, 0, sizeof(*out));
    auto registry = Union::StyleRegistry::instance();
    auto platform = registry->platform();
    out->smooth_scroll = platform ? (platform->smoothScroll() ? 1 : 0) : 1;
    out->animation_speed = platform ? float(platform->animationSpeedMultiplier()) : 1.0f;
    out->right_to_left = QGuiApplication::layoutDirection() == Qt::RightToLeft ? 1 : 0;
    const auto *iconTheme = KIconLoader::global()->theme();
    out->icon_theme_id = avu::internString(iconTheme ? iconTheme->internalName() : QIcon::themeName());
    auto style = registry->defaultStyle();
    out->default_style_id = avu::internString(style ? style->id() : QString{});
    return AVU_OK;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_font(int which, avu_font_info *out)
{
    AVU_GUARD_BEGIN
    if (!out || !avu::initialized()) {
        return AVU_E_ARG;
    }
    QFont font;
    switch (which) {
    case AVU_FONT_GENERAL:
        font = QGuiApplication::font();
        break;
    case AVU_FONT_FIXED:
        font = QFontDatabase::systemFont(QFontDatabase::FixedFont);
        break;
    case AVU_FONT_SMALL:
        font = QFontDatabase::systemFont(QFontDatabase::SmallestReadableFont);
        break;
    case AVU_FONT_MENU:
        font = kdeglobalsFont("menuFont");
        break;
    case AVU_FONT_TOOLBAR:
        font = kdeglobalsFont("toolBarFont");
        break;
    case AVU_FONT_TITLE:
        font = QFontDatabase::systemFont(QFontDatabase::TitleFont);
        break;
    default:
        return AVU_E_ARG;
    }
    out->family_id = avu::internString(font.family());
    out->point_size = float(font.pointSizeF());
    out->pixel_size = float(font.pixelSize());
    out->weight = int32_t(font.weight());
    out->italic = font.italic() ? 1 : 0;
    return AVU_OK;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}
