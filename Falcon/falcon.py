import sys
import requests  # pip install requests
from PyQt5.QtWidgets import (
    QApplication, QWidget, QLabel, QComboBox,
    QDateEdit, QPushButton, QVBoxLayout, QFormLayout
)
from PyQt5.QtCore import QDate

class MainWindow(QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Search for flights")

        # -- Main Vertical Layout --
        main_layout = QVBoxLayout()

        # -- Title Label --
        title_label = QLabel("Search for flights")
        font = title_label.font()
        font.setPointSize(14)
        font.setBold(True)
        title_label.setFont(font)
        main_layout.addWidget(title_label)

        # -- Form Layout for Origin/Destination/Date/Button --
        form_layout = QFormLayout()

        # 1) Origin combo box
        self.origin_combo = QComboBox()
        form_layout.addRow("Origin:", self.origin_combo)

        # 2) Destination combo box
        self.destination_combo = QComboBox()
        form_layout.addRow("Destination:", self.destination_combo)

        # 3) Departure date
        self.departure_date = QDateEdit()
        self.departure_date.setCalendarPopup(True)
        self.departure_date.setDate(QDate.currentDate())
        form_layout.addRow("Departure:", self.departure_date)

        # 4) Search button
        self.search_button = QPushButton("Search Now")
        form_layout.addRow(self.search_button)

        main_layout.addLayout(form_layout)
        self.setLayout(main_layout)

        # Load airport data from server
        self.fetch_airports()

        # Connect button signal to a slot
        self.search_button.clicked.connect(self.on_search_clicked)

    def fetch_airports(self):
        """Fetch airports from http://localhost/airports and populate combo boxes."""
        url = "http://localhost/airports"
        try:
            response = requests.get(url)
            response.raise_for_status()
            airports = response.json()  # list of dict

            # Fill origin and destination combo boxes
            for airport in airports:
                airport_id = airport["id"]
                airport_name = airport["name"]
                iata_code = airport["iataCode"]
                # timeZoneId = airport["timeZoneId"]  # if you need it

                display_text = f"{airport_name} ({iata_code})"

                # Add to Origin
                # We'll store the entire airport dict as userData if you need more info later
                self.origin_combo.addItem(display_text, airport)

                # Add to Destination
                self.destination_combo.addItem(display_text, airport)

        except requests.RequestException as e:
            print(f"Failed to fetch airports: {e}")

    def on_search_clicked(self):
        """Handle the 'Search Now' button click."""
        # Retrieve the stored airport dict from userData
        origin_airport = self.origin_combo.currentData()
        destination_airport = self.destination_combo.currentData()

        # You can directly extract the iataCode or other fields here
        origin_iata = origin_airport["iataCode"]
        destination_iata = destination_airport["iataCode"]

        departure_date_str = self.departure_date.date().toString("yyyy-MM-dd")

        # Here you could call another function or perform a search
        print(f"Searching flights from {origin_iata} to {destination_iata} on {departure_date_str}")

def main():
    app = QApplication(sys.argv)
    window = MainWindow()
    window.show()
    sys.exit(app.exec_())

if __name__ == "__main__":
    main()
