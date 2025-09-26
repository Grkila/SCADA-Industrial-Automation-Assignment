# Comprehensive SCADA System Project

A fully-featured SCADA (Supervisory Control and Data Acquisition) system developed in C# with .NET Framework, providing industrial process monitoring and control capabilities through a modern WPF interface.

## Overview

This project simulates a complete industrial SCADA environment with real-time data acquisition, alarm management, historical reporting, and process control capabilities. The system features a multi-layered architecture with a WPF graphical user interface, central data processing engine, and simulated PLC for realistic data generation.

### Key Highlights
- **Modern WPF Interface**: Intuitive tabbed interface with real-time data visualization
- **Real-Time Monitoring**: Live process data with manual control capabilities  
- **Comprehensive Alarm System**: Configurable alarms with historical tracking
- **Advanced Reporting**: Automated report generation with performance analysis
- **Robust Data Management**: SQL Server database with Entity Framework integration
- **Automated Testing**: Comprehensive test suite ensuring system reliability

## Table of Contents

- [System Architecture](#system-architecture)
- [Application Interface](#application-interface)
  - [Tags Management Tab](#tags-management-tab)
  - [Alarm Management Tab](#alarm-management-tab)
  - [Real-Time Monitoring Tab](#real-time-monitoring-tab)
  - [Alarm History Tab](#alarm-history-tab)
  - [Reports Tab](#reports-tab)
- [Core System Features](#core-system-features)
- [Technology Stack](#technology-stack)
- [Quick Start](#quick-start)

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

## Application Interface

The SCADA system features a modern WPF interface with five main tabs, each providing specific functionality for industrial process monitoring and control.

### Tags Management Tab
![Tags Management](Screenshots/Tags-tab.png)

The Tags tab is the foundation of the SCADA system, allowing users to configure and manage all data points in the industrial process.

**Key Features:**
- **Tag Types**: Support for four distinct tag types:
  - **DI (Digital Input)**: Binary state from field devices (switches, sensors)
  - **DO (Digital Output)**: Binary commands to field devices (lights, relays)
  - **AI (Analog Input)**: Continuous values from sensors (temperature, pressure, flow)
  - **AO (Analog Output)**: Continuous commands to actuators (valves, motors)

- **Smart Configuration**: The interface dynamically shows only relevant fields based on tag type:
  - **Common Properties**: Name, Description, I/O Address
  - **Input Properties**: Scan Time (ms), On/Off Scan toggle
  - **Analog Properties**: Low/High Limits, Units (°C, %, PSI, etc.)
  - **Output Properties**: Initial Value for system startup

- **Data Validation**: Comprehensive input validation ensures data integrity and prevents configuration errors

### Alarm Management Tab
![Alarm Management](Screenshots/Alarms-tab.png)

The Alarms tab provides comprehensive alarm configuration and management capabilities.

**Key Features:**
- **Alarm Configuration**: Create and manage alarms for Analog Input tags
- **Trigger Conditions**: Set alarms to trigger when values go Above or Below specified limits
- **Custom Messages**: Define descriptive alarm messages for each condition
- **Alarm Association**: Link alarms to specific AI tags for targeted monitoring

### Real-Time Monitoring Tab
![Real-Time Monitoring](Screenshots/Monitor-tab.png)

The Monitor tab serves as the main operational dashboard, providing real-time visibility into the industrial process.

**Key Features:**
- **Live Data Display**: Real-time grid showing all configured tags with current values
- **Manual Control**: Direct control of output tags (AO/DO) with value input validation
- **Active Alarms Panel**: Dedicated section displaying currently active alarms
- **Process Overview**: Complete visibility into system status and performance

### Alarm History Tab
![Alarm History](Screenshots/Alarm_history-tab.png)

The Alarm History tab provides comprehensive historical analysis of all alarm events.

**Key Features:**
- **Historical Records**: Complete log of all triggered alarms with timestamps
- **Event Details**: Detailed information including tag names, alarm messages, and trigger times
- **Data Analysis**: Historical patterns and trends for process optimization
- **Audit Trail**: Complete record for compliance and troubleshooting

### Reports Tab
![Reports Generation](Screenshots/Report-tab.png)

The Reports tab enables generation of detailed analytical reports for process optimization.

**Key Features:**
- **One-Click Generation**: Simple button interface for report creation
- **Performance Analysis**: Identifies periods of optimal operation within defined ranges
- **Ideal Range Calculation**: Uses formula `(high_limit + low_limit) / 2 ±5` for optimal performance detection
- **Timestamped Output**: Reports saved with unique filenames for easy organization

### Generated Report Example
![Generated Report](Screenshots/Generated-report_example.png)

Example of the generated report showing historical data analysis and optimal performance periods.

## Core System Features

### 1. Tag Management System

The core of the SCADA system is the ability to manage "tags," which represent individual data points from the industrial process.

**Tag Types and Properties:**
- **Digital Input (DI)**: Binary state from field devices (switches, sensors)
- **Digital Output (DO)**: Binary commands to field devices (lights, relays)  
- **Analog Input (AI)**: Continuous values from sensors (temperature, pressure, flow)
- **Analog Output (AO)**: Continuous commands to actuators (valves, motors)

**Configuration Features:**
- Dynamic UI that shows only relevant fields based on tag type
- Comprehensive input validation at both UI and data model levels
- Support for units, limits, scan times, and initial values
- Real-time validation prevents invalid configurations

### 2. Alarm Management System

Robust alarm system for critical process condition notification.

**Alarm Configuration:**
- Create alarms for any Analog Input tag
- Set trigger conditions (Above/Below thresholds)
- Custom alarm messages for each condition
- Real-time alarm evaluation and notification

**Alarm Lifecycle:**
1. Continuous monitoring of AI tag values against alarm limits
2. Automatic database logging when conditions are met
3. Real-time GUI updates for active alarms
4. Historical tracking for analysis and compliance

### 3. Real-Time Data Processing

**Data Flow Architecture:**
- PLC Simulator generates realistic process data
- DataConcentrator polls and processes data at configurable intervals
- Real-time alarm evaluation and event publishing
- Live GUI updates via event-driven architecture

**Control Capabilities:**
- Manual control of output tags with validation
- Real-time value updates and status monitoring
- Input validation for all control operations
- Thread-safe concurrent operations

### 4. Historical Data and Reporting

**Data Persistence:**
- SQL Server LocalDB for reliable data storage
- Entity Framework Code-First approach
- Complete audit trail of all system events
- Optimized queries for historical data access

**Report Generation:**
- Automated analysis of historical data
- Identification of optimal performance periods
- Timestamped report files for organization
- Performance metrics and trend analysis

### 5. Automated Testing Suite

Comprehensive testing framework ensuring system reliability.

**Test Coverage:**
- Database initialization and schema validation
- Complete CRUD operations for all entities
- Input validation and error handling
- Edge cases and boundary conditions
- Concurrent operations and thread safety
- Integration testing across all components

## Technology Stack

*   **Language**: C#
*   **Framework**: .NET Framework 4.7.2
*   **User Interface**: WPF (Windows Presentation Foundation) with MVVM
*   **Data Access**: Entity Framework 6 (Code-First)
*   **Database**: SQL Server LocalDB

## Quick Start

### Prerequisites

- **Visual Studio 2019 or newer** with .NET desktop development workload
- **SQL Server LocalDB** (included with Visual Studio)
- **Windows 10/11** (WPF requirement)

### Installation Steps

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd PSUSUproject
   ```

2. **Open in Visual Studio**
   - Open `PSUSUproject.sln` in Visual Studio
   - Allow NuGet package restoration when prompted

3. **Build the Solution**
   - Build > Build Solution (Ctrl+Shift+B)
   - Ensure all projects compile successfully

4. **Set Startup Project**
   - Right-click `ScadaGUI` project → "Set as Startup Project"

5. **Run the Application**
   - Press F5 or click "Start" button
   - Database will be automatically created on first run

### First Run Setup

1. **Configure Tags**: Navigate to the Tags tab and create your first data points
2. **Set Up Alarms**: Use the Alarms tab to configure monitoring thresholds  
3. **Start Monitoring**: Switch to the Monitor tab to view real-time data
4. **Generate Reports**: Use the Reports tab to create performance analysis

The system is now ready for industrial process simulation and monitoring!