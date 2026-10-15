import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

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
        Label { text: "Buttons"; font.bold: true }
        Flow {
            spacing: 8
            Layout.preferredWidth: 860
            Button { text: "Button" }
            Button { text: "Default"; highlighted: true }
            Button { text: "Flat"; flat: true }
            Button { text: "Disabled"; enabled: false }
            Button {
                id: withMenu
                text: "With menu"
                Accessible.role: Accessible.ButtonMenu
                onClicked: menu.popup(withMenu, 0, withMenu.height)
                property Menu menu: Menu { MenuItem { text: "One" } MenuItem { text: "Two" } }
            }
            Button { text: "Repeat"; autoRepeat: true }
            Button { text: "Toggle"; checkable: true }
            Button { text: "Checked"; checkable: true; checked: true }
        }
    }
}
