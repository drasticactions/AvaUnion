import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import org.kde.kirigami as Kirigami

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
        spacing: 10
        Label { text: "Session fonts"; font.bold: true }
        Label { text: "General font"; font: Kirigami.Theme.defaultFont }
        Label { text: "Fixed-width font"; font: Kirigami.Theme.fixedWidthFont }
        Label { text: "Small font"; font: Kirigami.Theme.smallFont }
        Label { text: "Icons"; font.bold: true }
        Flow {
            spacing: 12
            Layout.preferredWidth: 860
            Repeater {
                model: [["arrow-down", 16], ["arrow-up", 16], ["arrow-left", 16], ["arrow-right", 16], ["search", 16], ["edit-clear", 16],
                        ["help-contextual-symbolic", 16], ["window-close-symbolic", 16], ["application-menu-symbolic", 16], ["document-open", 22], ["folder", 32]]
                Kirigami.Icon { required property var modelData; source: modelData[0]; width: modelData[1]; height: modelData[1] }
            }
        }
    }
}
