#include <cstdio>
#include <cstdlib>
#include <string>
#include <vector>

#include <QDir>
#include <QFile>
#include <QTemporaryDir>

#include <QString>
#include <QStringList>

#include "avaunion.h"

namespace
{
QString arg(char **argv, int i)
{
    return QString::fromLocal8Bit(argv[i]);
}

const uint16_t *u16(const QString &s)
{
    return reinterpret_cast<const uint16_t *>(s.utf16());
}

int usage()
{
    std::fprintf(stderr,
                 "usage:\n"
                 "  union-export style <id> <out.json>\n"
                 "  union-export package <dir> <out.json>\n"
                 "  union-export scheme <file.colors> <out.json>\n"
                 "  union-export query <style-id> <chains.json> <out.json> [file.colors]\n"
                 "  union-export parity <chains.json> <out.json> <file.colors>...\n"
                 "\n"
                 "parity runs the styles in AVU_PARITY_STYLES (default breeze, breeze-rtl,\n"
                 "breeze-mobile) and the package folders in AVU_PARITY_PACKAGES, both\n"
                 "comma-separated. When only AVU_PARITY_PACKAGES is set, it runs the packages.\n");
    return 2;
}

avu_style styleById(const QString &id)
{
    auto style = avu_style_by_id(u16(id), int(id.size()));
    if (!style) {
        std::fprintf(stderr, "union-export: no style '%s'\n", qPrintable(id));
    }
    return style;
}

avu_style styleFromPackage(const QString &dir)
{
    avu_style style = nullptr;
    if (int status = avu_style_from_package(u16(dir), int(dir.size()), &style); status != AVU_OK) {
        std::fprintf(stderr, "union-export: package '%s' failed (%d)\n", qPrintable(dir), status);
        return nullptr;
    }
    return style;
}
}

int main(int argc, char **argv)
{
    if (argc < 2) {
        return usage();
    }
    const auto command = arg(argv, 1);

    avu_probe_result probe{};
    if (int status = avu_probe(&probe); status != AVU_OK) {
        std::fprintf(stderr, "union-export: probe failed (%d): shim %s, loaded %s\n", status, probe.shim_union_version, probe.loaded_union_version);
        return 1;
    }

    QTemporaryDir cache;
    if (!std::getenv("AVU_EXPORT_KEEP_CACHE") && cache.isValid()) {
        setenv("XDG_CACHE_HOME", QFile::encodeName(cache.path()).constData(), 1);
    }

    avu_init_options options{};
    options.force_platform = 2;
    avu_init_result init{};
    if (int status = avu_init(&options, &init); status != AVU_OK) {
        std::fprintf(stderr, "union-export: init failed (%d)\n", status);
        return 1;
    }

    int status = AVU_E_ARG;
    if (command == u"style" && argc == 4) {
        if (auto style = styleById(arg(argv, 2))) {
            const auto out = arg(argv, 3);
            status = avu_export_style(style, u16(out), int(out.size()));
        }
    } else if (command == u"package" && argc == 4) {
        if (auto style = styleFromPackage(arg(argv, 2))) {
            const auto out = arg(argv, 3);
            status = avu_export_style(style, u16(out), int(out.size()));
        }
    } else if (command == u"scheme" && argc == 4) {
        const auto colors = arg(argv, 2);
        const auto out = arg(argv, 3);
        status = avu_export_scheme(u16(colors), int(colors.size()), u16(out), int(out.size()));
    } else if (command == u"query" && (argc == 5 || argc == 6)) {
        if (argc == 6) {
            const auto colors = arg(argv, 5);
            avu_set_color_scheme_path(u16(colors), int(colors.size()));
        }
        if (auto style = styleById(arg(argv, 2))) {
            const auto chains = arg(argv, 3);
            const auto out = arg(argv, 4);
            status = avu_export_query(style, u16(chains), int(chains.size()), u16(out), int(out.size()));
        }
    } else if (command == u"parity" && argc >= 5) {
        QStringList styles = QString::fromLocal8Bit(qgetenv("AVU_PARITY_STYLES")).split(u',', Qt::SkipEmptyParts);
        const QStringList packages = QString::fromLocal8Bit(qgetenv("AVU_PARITY_PACKAGES")).split(u',', Qt::SkipEmptyParts);
        if (styles.isEmpty() && packages.isEmpty()) {
            styles = {QStringLiteral("breeze"), QStringLiteral("breeze-rtl"), QStringLiteral("breeze-mobile")};
        }
        std::vector<avu_style> handles;
        bool ok = true;
        for (const auto &id : styles) {
            auto style = styleById(id);
            ok = ok && style;
            handles.push_back(style);
        }
        for (const auto &dir : packages) {
            auto style = styleFromPackage(dir);
            ok = ok && style;
            handles.push_back(style);
        }
        QStringList colors;
        for (int i = 4; i < argc; ++i) {
            colors.append(arg(argv, i));
        }
        const auto colorList = colors.join(u'\n');
        const auto chains = arg(argv, 2);
        const auto out = arg(argv, 3);
        if (ok) {
            status = avu_export_parity_handles(handles.data(), int(handles.size()), u16(colorList), int(colorList.size()), u16(chains), int(chains.size()), u16(out), int(out.size()));
        }
    } else {
        avu_shutdown();
        return usage();
    }

    if (status != AVU_OK) {
        std::fprintf(stderr, "union-export: %s failed (%d)\n", qPrintable(command), status);
    }
    avu_shutdown();
    return status == AVU_OK ? 0 : 1;
}
