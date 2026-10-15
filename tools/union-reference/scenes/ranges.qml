import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import org.kde.union.impl as Union

Pane {
    LayoutMirroring.enabled: Qt.application.layoutDirection === Qt.RightToLeft
    LayoutMirroring.childrenInherit: true
    width: 900
    height: 700
    leftPadding: 16
    rightPadding: 16
    topPadding: 16
    bottomPadding: 16
    ColumnLayout {
        spacing: 16
        Label { text: "Sliders"; font.bold: true }
        Slider { Layout.preferredWidth: 260; from: 0; to: 100; value: 35 }
        Slider { Layout.preferredWidth: 260; from: 0; to: 100; value: 70; enabled: false }
        Slider { orientation: Qt.Vertical; Layout.preferredHeight: 140; from: 0; to: 100; value: 50 }
        Label { text: "Progress bars"; font.bold: true }
        ProgressBar { Layout.preferredWidth: 260; from: 0; to: 100; value: 45
            Union.Element.hints: Union.ElementHint { name: "horizontal" } }
        ProgressBar { Layout.preferredWidth: 260; from: 0; to: 100; value: 80
            Union.Element.hints: Union.ElementHint { name: "horizontal" } }
        ProgressBar { Layout.preferredWidth: 260; indeterminate: true
            Union.Element.hints: [Union.ElementHint { name: "horizontal" }, Union.ElementHint { name: "indeterminate" }] }
        Label { text: "Scroll bars"; font.bold: true }
        ScrollBar { orientation: Qt.Horizontal; Layout.preferredWidth: 260; size: 0.3; policy: ScrollBar.AlwaysOn; active: true }
    }
}
