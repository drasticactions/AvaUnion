#include <cstring>
#include <dlfcn.h>
#include <vector>
#include <filesystem>
#include <limits.h>
#include <stdlib.h>
#include <string>
#include <sys/syscall.h>
#include <unistd.h>

#include <QAbstractEventDispatcher>
#include <QCoreApplication>
#include <QFileInfo>
#include <QGuiApplication>
#include <QLibraryInfo>
#include <QThread>

#include "Common.h"
#include "StyleRegistry.h"
#include "UnionSchemas.g.h"

#ifndef AVU_UNION_VERSION
#error "AVU_UNION_VERSION must be defined by the build"
#endif

namespace
{
bool s_initialized = false;
bool s_createdApplication = false;
QGuiApplication *s_application = nullptr;
avu_style_s s_defaultStyle{nullptr, true};

int s_argc = 0;
char *s_argv[8] = {};
std::string s_argStorage[8];

void copyVersion(char *dest, const std::string &value)
{
    const auto n = std::min<size_t>(value.size(), 31);
    std::memcpy(dest, value.data(), n);
    dest[n] = '\0';
}

std::string loadedUnionVersion()
{
    Dl_info info{};
    auto symbol = reinterpret_cast<void *>(&Union::StyleRegistry::instance);
    if (dladdr(symbol, &info) == 0 || !info.dli_fname) {
        return {};
    }
    char resolved[PATH_MAX];
    if (!realpath(info.dli_fname, resolved)) {
        return {};
    }
    const std::string name = std::filesystem::path(resolved).filename().string();
    const std::string marker = ".so.";
    const auto pos = name.find(marker);
    if (pos == std::string::npos) {
        return {};
    }
    return name.substr(pos + marker.size());
}

struct Version {
    int major = -1;
    int minor = -1;
    int patch = -1;
};

Version parseVersion(const std::string &value)
{
    Version v;
    std::sscanf(value.c_str(), "%d.%d.%d", &v.major, &v.minor, &v.patch);
    return v;
}

const char *schemaOf(const std::string &version)
{
    for (const auto &release : avu_union_releases) {
        if (version == release.version) {
            return release.schema;
        }
    }
    const auto v = parseVersion(version);
    if (v.major < 0 || v.patch < 0 || v.patch >= 80) {
        return nullptr;
    }
    const char *found = nullptr;
    int foundPatch = -1;
    for (const auto &release : avu_union_releases) {
        const auto r = parseVersion(release.version);
        if (r.major == v.major && r.minor == v.minor && r.patch < 80 && r.patch > foundPatch) {
            found = release.schema;
            foundPatch = r.patch;
        }
    }
    return found;
}

bool versionsCompatible(const std::string &shim, const std::string &loaded)
{
    if (parseVersion(shim).major < 0 || parseVersion(loaded).major < 0) {
        return false;
    }
    if (shim == loaded) {
        return true;
    }
    const char *schema = schemaOf(loaded);
    return schema && std::strcmp(schema, AVU_UNION_SCHEMA) == 0;
}

bool kdePlatformThemeAvailable()
{
    const auto dir = QLibraryInfo::path(QLibraryInfo::PluginsPath) + QStringLiteral("/platformthemes/KDEPlasmaPlatformTheme6.so");
    return QFileInfo::exists(dir);
}

bool isPlasmaSession()
{
    const auto desktop = qEnvironmentVariable("XDG_CURRENT_DESKTOP");
    return desktop.split(u':').contains(QStringLiteral("KDE"), Qt::CaseInsensitive);
}

bool onProcessMainThread()
{
    return pid_t(syscall(SYS_gettid)) == getpid();
}
}

bool avu::initialized()
{
    return s_initialized;
}

avu_style avu::defaultStyleHandle()
{
    return &s_defaultStyle;
}

void avu::refreshDefaultStyle()
{
    s_defaultStyle.style = Union::StyleRegistry::instance()->defaultStyle();
}

extern "C" AVU_API int avu_probe(avu_probe_result *out)
{
    AVU_GUARD_BEGIN
    if (!out) {
        return AVU_E_ARG;
    }
    std::memset(out, 0, sizeof(*out));
    out->abi_version = AVU_ABI_VERSION;
    out->resolved_size = int32_t(sizeof(avu_resolved));
    copyVersion(out->shim_union_version, AVU_UNION_VERSION);
    const auto loaded = loadedUnionVersion();
    copyVersion(out->loaded_union_version, loaded);
    if (loaded.empty()) {
        return AVU_E_NO_UNION;
    }
    if (!versionsCompatible(AVU_UNION_VERSION, loaded)) {
        return AVU_E_VERSION;
    }
    return AVU_OK;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API int avu_init(const avu_init_options *opts, avu_init_result *out)
{
    AVU_GUARD_BEGIN
    if (!out) {
        return AVU_E_ARG;
    }
    std::memset(out, 0, sizeof(*out));
    avu_init_options options{};
    if (opts) {
        options = *opts;
    }

    if (s_initialized) {
        return AVU_E_ARG;
    }

    bool platformThemeLoaded = false;
    if (auto existing = QCoreApplication::instance()) {
        auto gui = qobject_cast<QGuiApplication *>(existing);
        if (!gui || existing->thread() != QThread::currentThread()) {
            return AVU_E_FOREIGN_QAPP;
        }
        s_application = gui;
        platformThemeLoaded = QGuiApplication::platformName().size() > 0 && kdePlatformThemeAvailable();
    } else {
        std::vector<std::string> args{"avaunion", "-platform", "offscreen"};
        if (kdePlatformThemeAvailable()) {
            args.push_back("-platformtheme");
            args.push_back("kde");
            platformThemeLoaded = true;
        }
        s_argc = int(args.size());
        for (size_t i = 0; i < args.size(); ++i) {
            s_argStorage[i] = args[i];
            s_argv[i] = s_argStorage[i].data();
        }
        s_argv[args.size()] = nullptr;
        s_application = new QGuiApplication(s_argc, s_argv);
        s_createdApplication = true;
    }

    const bool forcePlasma = options.force_platform == 1 || (options.force_platform == 0 && isPlasmaSession());
    const QByteArray previous = qgetenv("UNION_FORCE_PLATFORM");
    const bool hadPrevious = qEnvironmentVariableIsSet("UNION_FORCE_PLATFORM");
    if (forcePlasma) {
        qputenv("UNION_FORCE_PLATFORM", "plasma");
    } else if (options.force_platform == 2) {
        qunsetenv("UNION_FORCE_PLATFORM");
    }
    auto registry = Union::StyleRegistry::instance();
    if (hadPrevious) {
        qputenv("UNION_FORCE_PLATFORM", previous);
    } else {
        qunsetenv("UNION_FORCE_PLATFORM");
    }

    registry->load();

    s_initialized = true;
    avu::refreshDefaultStyle();

    avu::installEventWatchers(platformThemeLoaded);

    const auto dispatcherName = QString::fromLatin1(QCoreApplication::eventDispatcher() ? QCoreApplication::eventDispatcher()->metaObject()->className() : "");
    const bool glibDispatcher = dispatcherName.contains(QStringLiteral("Glib"), Qt::CaseInsensitive);
    out->pump_mode = (options.host_runs_glib && onProcessMainThread() && glibDispatcher) ? AVU_PUMP_SHARED : AVU_PUMP_TIMER;
    out->platform_theme_loaded = platformThemeLoaded ? 1 : 0;
    out->created_application = s_createdApplication ? 1 : 0;
    auto platform = registry->platform();
    out->platform_plugin_id = avu::internString(platform ? QString::fromLatin1(platform->metaObject()->className()) : QString{});
    out->default_style_id = avu::internString(s_defaultStyle.style ? s_defaultStyle.style->id() : QString{});
    out->qt_version_id = avu::internString(QString::fromLatin1(qVersion()));
    out->union_version_id = avu::internString(QString::fromStdString(loadedUnionVersion()));

    if (!s_defaultStyle.style) {
        return AVU_E_NO_STYLE;
    }
    return AVU_OK;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}

extern "C" AVU_API void avu_shutdown(void)
{
    AVU_GUARD_BEGIN
    if (!s_initialized) {
        return;
    }
    Union::StyleRegistry::instance()->save();
    s_defaultStyle.style.reset();
    s_initialized = false;
    if (s_createdApplication) {
        delete s_application;
        s_application = nullptr;
        s_createdApplication = false;
    }
    AVU_GUARD_END_VOID
}
