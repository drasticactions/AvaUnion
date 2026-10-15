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
        Label { text: "Check boxes"; font.bold: true }
        ColumnLayout {
            spacing: 4
            CheckBox { text: "Unchecked" }
            CheckBox { text: "Checked"; checked: true }
            CheckBox { text: "Indeterminate"; tristate: true; checkState: Qt.PartiallyChecked }
            CheckBox { text: "Disabled"; enabled: false }
            CheckBox { text: "Disabled and checked"; enabled: false; checked: true }
        }
        Label { text: "Radio buttons"; font.bold: true }
        ColumnLayout {
            spacing: 4
            RadioButton { text: "First"; checked: true }
            RadioButton { text: "Second" }
            RadioButton { text: "Disabled"; enabled: false }
        }
        Label { text: "Switches"; font.bold: true }
        ColumnLayout {
            spacing: 4
            Switch { text: "Off" }
            Switch { text: "On"; checked: true }
            Switch { text: "Disabled"; enabled: false }
        }
    }
}
