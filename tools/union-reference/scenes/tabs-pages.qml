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
        width: 520
        Label { text: "Tab control"; font.bold: true }
        ColumnLayout {
            spacing: 0
            Layout.fillWidth: true
            TabBar {
                id: tabs
                Layout.fillWidth: true
                TabButton { text: "General" }
                TabButton { text: "Appearance" }
                TabButton { text: "Advanced" }
            }
            Frame {
                Layout.fillWidth: true
                Layout.preferredHeight: 100
                Label { text: "General settings" }
            }
        }
        Label { text: "Expander"; font.bold: true }
        ToolButton { text: "More options"; icon.name: "arrow-right" }
        ToolButton { text: "Expanded"; icon.name: "arrow-down" }
        ColumnLayout {
            spacing: 4
            CheckBox { text: "Option one" }
            CheckBox { text: "Option two" }
        }
        Label { text: "Group box"; font.bold: true }
        GroupBox {
            title: "Notifications"
            Layout.fillWidth: true
            ColumnLayout {
                spacing: 4
                Switch { text: "Show banners"; checked: true }
                Switch { text: "Play sounds" }
            }
        }
        Label { text: "Split view"; font.bold: true }
        Row {
            Union.StyledRectangle {
                Union.Element.type: "Drawer"
                Union.Element.attributes: Union.ElementAttribute { name: "edge"; value: "left" }
                width: 140
                height: 120
                Label { x: 8; y: 8; text: "Drawer" }
            }
            Item {
                width: 380
                height: 120
                Label { x: 8; y: 8; text: "Content" }
            }
        }
    }
}
