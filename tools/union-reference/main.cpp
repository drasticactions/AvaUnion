#include <QCommandLineParser>
#include <QDir>
#include <QEvent>
#include <QFileInfo>
#include <QGuiApplication>
#include <QQmlEngine>
#include <QQuickItem>
#include <QQuickStyle>
#include <QQuickView>
#include <QTimer>

int main(int argc, char **argv)
{
    qputenv("QT_QUICK_CONTROLS_STYLE", "org.kde.union");
    if (qEnvironmentVariableIsEmpty("QT_QPA_PLATFORMTHEME")) {
        qputenv("QT_QPA_PLATFORMTHEME", "kde");
    }
    for (int i = 1; i + 1 < argc; ++i) {
        if (qstrcmp(argv[i], "--scale") == 0) {
            qputenv("QT_SCALE_FACTOR", argv[i + 1]);
        }
    }
    QGuiApplication app(argc, argv);

    QCommandLineParser parser;
    parser.addPositionalArgument(QStringLiteral("out"), QStringLiteral("Output folder"));
    parser.addPositionalArgument(QStringLiteral("scenes"), QStringLiteral("Scene QML files"), QStringLiteral("[scene.qml...]"));
    QCommandLineOption scaleOption(QStringLiteral("scale"), QStringLiteral("Device pixel ratio"), QStringLiteral("scale"), QStringLiteral("1"));
    QCommandLineOption schemeOption(QStringLiteral("scheme"), QStringLiteral("A .colors file for this application"), QStringLiteral("scheme"));
    parser.addOption(scaleOption);
    parser.addOption(schemeOption);
    parser.addHelpOption();
    parser.process(app);

    const auto args = parser.positionalArguments();
    if (args.size() < 2) {
        parser.showHelp(2);
    }

    if (parser.isSet(schemeOption)) {
        app.setProperty("KDE_COLOR_SCHEME_PATH", QFileInfo(parser.value(schemeOption)).absoluteFilePath());
        QEvent event(QEvent::ApplicationPaletteChange);
        QCoreApplication::sendEvent(&app, &event);
    }

    QQuickStyle::setStyle(QStringLiteral("org.kde.union"));
    const QDir out(args.at(0));
    out.mkpath(QStringLiteral("."));

    int failures = 0;
    for (int i = 1; i < args.size(); ++i) {
        const QFileInfo scene(args.at(i));
        QQuickView view;
        view.setResizeMode(QQuickView::SizeViewToRootObject);
        view.setSource(QUrl::fromLocalFile(scene.absoluteFilePath()));
        if (view.status() != QQuickView::Ready) {
            for (const auto &error : view.errors()) {
                qWarning().noquote() << error.toString();
            }
            ++failures;
            continue;
        }
        view.show();

        for (int frame = 0; frame < 10; ++frame) {
            QCoreApplication::processEvents(QEventLoop::AllEvents, 20);
        }

        const QImage image = view.grabWindow();
        const auto path = out.filePath(scene.completeBaseName() + QStringLiteral(".png"));
        if (image.isNull() || !image.save(path)) {
            qWarning().noquote() << "could not render" << scene.fileName();
            ++failures;
            continue;
        }
        qInfo().noquote() << path;
    }

    return failures == 0 ? 0 : 1;
}
