#include <QCoreApplication>
#include <QDBusConnection>
#include <QEvent>
#include <QGuiApplication>
#include <QMetaObject>
#include <QObject>
#include <QPointer>

#include <KIconLoader>

#include "Common.h"
#include "PlatformPlugin.h"
#include "StyleRegistry.h"

namespace
{
avu_callback s_callback = nullptr;
void *s_callbackUser = nullptr;
uint32_t s_pendingFlags = 0;
bool s_dispatchQueued = false;
QtMessageHandler s_previousHandler = nullptr;

class Watcher : public QObject
{
    Q_OBJECT

public:
    using QObject::QObject;

    bool eventFilter(QObject *watched, QEvent *event) override
    {
        const auto type = event->type();
        if (watched == qApp) {
            if (type == QEvent::ApplicationPaletteChange) {
                avu::notifyChange(AVU_CHANGE_COLORS);
            } else if (type == QEvent::ApplicationFontChange) {
                avu::notifyChange(AVU_CHANGE_FONTS);
            } else if (type == QEvent::ThemeChange) {
                avu::notifyChange(AVU_CHANGE_ICONS);
            } else if (type == QEvent::ApplicationLayoutDirectionChange) {
                avu::notifyChange(AVU_CHANGE_STYLE);
            }
        } else if (type == Union::DefaultStyleChangedEvent::s_type) {
            avu::notifyChange(AVU_CHANGE_STYLE);
        } else if (type == Union::AnimationSpeedMultiplierChangedEvent::s_type) {
            avu::notifyChange(AVU_CHANGE_ANIMATION);
        }
        return QObject::eventFilter(watched, event);
    }

    Q_SLOT void globalSettingsChanged(int type, int arg)
    {
        Q_UNUSED(arg);
        constexpr int PaletteChanged = 0;
        constexpr int FontChanged = 1;
        constexpr int IconChanged = 4;
        if (type == PaletteChanged) {
            QEvent event(QEvent::ApplicationPaletteChange);
            QCoreApplication::sendEvent(qApp, &event);
        } else if (type == FontChanged) {
            avu::notifyChange(AVU_CHANGE_FONTS);
        } else if (type == IconChanged) {
            avu::notifyChange(AVU_CHANGE_ICONS);
        }
    }

    Q_SLOT void smoothScrollChanged()
    {
        avu::notifyChange(AVU_CHANGE_ANIMATION);
    }
};

QPointer<Watcher> s_watcher;

void dispatch()
{
    s_dispatchQueued = false;
    const auto flags = s_pendingFlags;
    s_pendingFlags = 0;
    if (flags & AVU_CHANGE_STYLE) {
        avu::refreshDefaultStyle();
    }
    if (s_callback && flags) {
        s_callback(flags, 0, s_callbackUser);
    }
}

void messageHandler(QtMsgType type, const QMessageLogContext &context, const QString &message)
{
    const auto category = QString::fromLatin1(context.category ? context.category : "");
    if (category.startsWith(QStringLiteral("union")) && s_callback && type != QtDebugMsg && type != QtInfoMsg) {
        avu::notifyLog(category + QStringLiteral(": ") + message);
        return;
    }
    if (s_previousHandler) {
        s_previousHandler(type, context, message);
    }
}
}

void avu::notifyChange(uint32_t flags)
{
    s_pendingFlags |= flags;
    if (!s_dispatchQueued && s_watcher) {
        s_dispatchQueued = true;
        QMetaObject::invokeMethod(s_watcher.data(), &dispatch, Qt::QueuedConnection);
    }
}

void avu::notifyLog(const QString &message)
{
    if (s_callback) {
        s_callback(AVU_CHANGE_LOG, avu::internString(message), s_callbackUser);
    }
}

void avu::installEventWatchers(bool platformThemeLoaded)
{
    if (s_watcher) {
        return;
    }
    s_watcher = new Watcher(qApp);
    qApp->installEventFilter(s_watcher);

    if (auto platform = Union::StyleRegistry::instance()->platform()) {
        platform->installEventFilter(s_watcher);
        QObject::connect(platform.get(), &Union::PlatformPlugin::smoothScrollChanged, s_watcher.data(), &Watcher::smoothScrollChanged);
    }

    QObject::connect(KIconLoader::global(), &KIconLoader::iconLoaderSettingsChanged, s_watcher.data(), [] {
        avu::notifyChange(AVU_CHANGE_ICONS);
    });

    if (!platformThemeLoaded) {
        QDBusConnection::sessionBus().connect(QString(),
                                              QStringLiteral("/KGlobalSettings"),
                                              QStringLiteral("org.kde.KGlobalSettings"),
                                              QStringLiteral("notifyChange"),
                                              s_watcher.data(),
                                              SLOT(globalSettingsChanged(int, int)));
    }

    s_previousHandler = qInstallMessageHandler(messageHandler);
}

extern "C" AVU_API void avu_set_callback(avu_callback cb, void *user)
{
    s_callback = cb;
    s_callbackUser = user;
}

#include "Events.moc"
