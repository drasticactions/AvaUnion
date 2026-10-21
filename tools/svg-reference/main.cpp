#include <QFile>
#include <QGuiApplication>
#include <QImage>
#include <QPainter>
#include <QSvgRenderer>
#include <QTextStream>

int main(int argc, char **argv)
{
    qputenv("QT_QPA_PLATFORM", "offscreen");
    QGuiApplication app(argc, argv);
    if (argc != 2) {
        QTextStream(stderr) << "usage: svg-reference <list>, one \"<pixels> <in.svg> <out.png>\" per line\n";
        return 2;
    }

    QFile list(QString::fromLocal8Bit(argv[1]));
    if (!list.open(QIODevice::ReadOnly)) {
        return 1;
    }

    QTextStream in(&list);
    while (!in.atEnd()) {
        const auto parts = in.readLine().split(QLatin1Char(' '));
        if (parts.size() != 3) {
            continue;
        }

        const int pixels = parts[0].toInt();
        QSvgRenderer renderer(parts[1]);
        QImage image(pixels, pixels, QImage::Format_ARGB32_Premultiplied);
        image.fill(Qt::transparent);
        if (renderer.isValid()) {
            QPainter painter(&image);
            renderer.render(&painter, QRectF(0, 0, pixels, pixels));
        }

        image.save(parts[2]);
    }

    return 0;
}
