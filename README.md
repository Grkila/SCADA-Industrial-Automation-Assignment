# Comprehensive SCADA System Project

This repository contains a fully-featured SCADA (Supervisory Control and Data Acquisition) system developed in C# with the .NET Framework. The project provides a robust simulation of industrial process monitoring and control, featuring a WPF graphical user interface, a central data processing engine, and a simulated PLC for data generation. The system's entire configuration and operational history are persisted in a SQL Server database using Entity Framework.

## System Architecture

The application is built on a decoupled, multi-project architecture designed for clarity and maintainability.

*   **ScadaGUI (WPF Application)**: This is the primary user interface. Built using the MVVM (Model-View-ViewModel) design pattern, it allows users to configure the system, monitor live data, and manually control output devices. It acts as a subscriber to events published by the `DataConcentrator`, ensuring the UI reflects real-time system states and alarms.

*   **DataConcentrator (Core Logic)**: This class library is the brain of the system. It holds the current values and configurations for all tags and alarms. Its responsibilities include:
    *   Polling the `PLCSimulator` at intervals defined by each tag's scan time.
    *   Evaluating tag values against their configured alarm thresholds.
    *   Writing alarm events to the database when they occur.
    *   Publishing events to notify the GUI of new alarms.
    *   Handling read/write requests for configuration data from the database.

*   **PLCSimulator (Data Source)**: This library simulates a Programmable Logic Controller. It runs on a background thread, continuously generating dynamic data for a set of I/O addresses. This provides realistic, changing values for the `DataConcentrator` to read, mimicking a live industrial environment with active sensors.

*   **Database (Persistence Layer)**: A SQL Server LocalDB instance serves as the data store. The project uses an Entity Framework Code-First approach, meaning the database schema is generated directly from the C# data models. It contains three essential tables:
    1.  `Tags`: Stores the configuration for every digital and analog tag.
    2.  `Alarms`: Stores the definitions for all alarms, linked to specific tags.
    3.  `ActivatedAlarms`: A historical log of every alarm that has been triggered, including its message and a timestamp.

## Features in Detail

### 1. Tag Management

The core of the SCADA system is the ability to manage "tags," which represent individual data points from the industrial process.

*   **Tag Types**: The system supports four distinct types of tags:
    *   **DI (Digital Input)**: Represents a binary state from the field, such as a switch being on or off.
    *   **DO (Digital Output)**: Allows the system to send a binary command, like turning a light on or off.
    *   **AI (Analog Input)**: Represents a continuous value from a sensor, such as temperature or pressure.
    *   **AO (Analog Output)**: Allows the system to send a continuous value, like setting a valve's position.

*   **Tag Properties**: Each tag is defined by a set of properties. The user interface intelligently displays only the fields relevant to the selected tag type.
    *   **Common Properties**: `Name` (ID), `Description`, `I/O Address`.
    *   **Input-Only Properties**: `Scan Time` (in milliseconds) and a toggle for `On/Off Scan`.
    *   **Analog-Only Properties**: `Low Limit`, `High Limit`, and `Units` (e.g., °C, %, PSI).
    *   **Output-Only Properties**: `Initial Value` to be set upon system start.

*   **Input Validation**: The system enforces strict validation rules at both the UI and data model levels to ensure data integrity. For example, a user cannot assign units to a digital tag or set a scan time for an output tag. Numerical fields also validate for correct formatting.

### 2. Alarm Management

The system provides a robust mechanism for notifying users of critical process conditions.

*   **Alarm Configuration**: Alarms can be created and associated with any Analog Input (AI) tag. Each alarm is defined by:
    *   **Trigger Type**: Activates when the tag's value goes `Above` or `Below` a certain point.
    *   **Limit**: The numerical threshold that triggers the alarm.
    *   **Message**: A custom text message to be displayed when the alarm is triggered.

*   **Alarm Lifecycle**:
    1.  The `DataConcentrator` continuously compares the current value of each AI tag against its associated alarm limits.
    2.  If a condition is met, a new entry is created in the `ActivatedAlarms` table in the database, capturing the alarm ID, tag name, message, and the exact time of the event.
    3.  An event is published to the `ScadaGUI`, which then displays the new active alarm in the Monitor tab.

### 3. Real-Time Monitoring and Control

The "Monitor" tab serves as the main dashboard for observing and interacting with the live system.

*   **Live Tag Values**: A data grid displays all configured tags, showing their real-time values as they are updated by the `DataConcentrator`.
*   **Manual Control**: When an output tag (AO or DO) is selected in the grid, an input panel appears. The user can enter a value and write it directly to the PLC Simulator. The system validates the input to ensure it is within the tag's defined limits (for AO) or is a valid binary value (0 or 1 for DO).
*   **Active Alarms Display**: A dedicated panel on the Monitor screen shows a list of currently active alarms, providing immediate feedback on critical events.

### 4. Reporting

The system includes a feature for generating historical data reports.

*   **Report Generation**: On the "Reports" tab, a user can click a button to generate a `.txt` file.
*   **Report Content**: The report analyzes the historical data for all Analog Input tags and lists every recorded value that fell within an "ideal" operational range, which is calculated as: `(high_limit + low_limit) / 2 ±5`. This helps identify periods of stable and optimal process performance.
*   **File Output**: Each report is saved with a unique, timestamped filename (e.g., `Report_20250926_030100.txt`).

### 5. Automated Testing

A separate console project, `tester`, is included to ensure the reliability of the core logic. It performs a comprehensive suite of tests, including:
*   **Database Initialization**: Verifies that the database is created correctly.
*   **CRUD Operations**: Tests the creation, reading, and deletion of tags and alarms.
*   **Validation Logic**: Confirms that invalid data and configurations are correctly rejected.
*   **Edge Cases**: Checks for proper handling of null inputs, duplicate entries, and operations on non-existent tags.
*   **Concurrent Operations**: Simulates multiple threads accessing the `DataConcentrator` to test for thread safety.

## Technology Stack

*   **Language**: C#
*   **Framework**: .NET Framework 4.7.2
*   **User Interface**: WPF (Windows Presentation Foundation) with MVVM
*   **Data Access**: Entity Framework 6 (Code-First)
*   **Database**: SQL Server LocalDB

## Getting Started

### Prerequisites

*   Visual Studio 2019 or newer
*   .NET desktop development workload
*   SQL Server LocalDB (usually installed with Visual Studio)

### Installation and Execution

1.  Clone the repository to your local machine.
2.  Open the `PSUSUproject.sln` solution file in Visual Studio.
3.  Build the solution (Build > Build Solution). This will automatically restore the necessary NuGet packages.
4.  In the Solution Explorer, right-click the `ScadaGUI` project and select "Set as Startup Project".
5.  Press F5 or click the "Start" button to run the application.

On the first run, Entity Framework will automatically create the `ScadaDatabase` in your local SQL Server LocalDB instance. You can then begin configuring tags and alarms.