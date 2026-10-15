#include <QAbstractEventDispatcher>
#include <QCoreApplication>
#include <QElapsedTimer>

#include "Common.h"

extern "C" AVU_API int avu_pump(int max_ms)
{
    AVU_GUARD_BEGIN
    if (!avu::initialized()) {
        return AVU_E_NOT_INIT;
    }
    auto dispatcher = QCoreApplication::eventDispatcher();
    if (!dispatcher) {
        return 0;
    }
    QElapsedTimer timer;
    timer.start();
    int processed = 0;
    do {
        QCoreApplication::sendPostedEvents();
        if (!dispatcher->processEvents(QEventLoop::AllEvents)) {
            break;
        }
        ++processed;
    } while (timer.elapsed() < std::max(0, max_ms));
    return processed;
    AVU_GUARD_END(AVU_E_EXCEPTION)
}
