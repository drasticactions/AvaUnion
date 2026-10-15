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
        Label { text: "Calendar"; font.bold: true }
        ColumnLayout {
            spacing: 0
            Label { text: "October 2026"; Layout.alignment: Qt.AlignHCenter }
            DayOfWeekRow { Layout.fillWidth: true; Layout.preferredWidth: 280 }
            MonthGrid { month: Calendar.October; year: 2026; Layout.preferredWidth: 280 }
        }
        Label { text: "Date and time pickers"; font.bold: true }
        RowLayout {
            spacing: 4
            Tumbler { model: 31; currentIndex: 13; visibleItemCount: 3 }
            Tumbler { model: ["September", "October", "November"]; currentIndex: 1; visibleItemCount: 3 }
            Tumbler { model: ["2025", "2026", "2027"]; currentIndex: 1; visibleItemCount: 3 }
        }
        ComboBox { model: ["10/14/2026"]; Layout.preferredWidth: 220 }
    }
}
