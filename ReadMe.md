# AISPubSub

> Aveva Integration Service Client application to Test the API
AISPubSub is a .NET application designed to provide robust, real-time logging and message publishing/subscribing capabilities. It leverages Serilog for structured logging and features a custom sink to display log events in a Windows Forms `ListBox` control, making it ideal for monitoring and debugging in GUI-based applications.

![License](https://img.shields.io/badge/license-MIT-green) ![Version](https://img.shields.io/badge/version-1.0.0-blue) ![Language](https://img.shields.io/badge/language-CSharp-yellow) ![Framework](https://img.shields.io/badge/framework-.net8-orange) ![GitHub](https://img.shields.io/badge/GitHub-keattavan2008/AISPubSub-black?logo=github) ![Build Status](https://img.shields.io/github/actions/workflow/status/keattavan2008/AISPubSub/ci.yml?branch=main)

## 📋 Table of Contents

- [Features](#features)
- [Installation](#installation)

## ℹ️ Project Information

- **👤 Author:** keattavan2008
- **📦 Version:** 1.0.0
- **📄 License:** MIT
- **📂 Repository:** [https://github.com/keattavan2008/AISPubSub](https://github.com/keattavan2008/AISPubSub)
- **🏷️ Keywords:** AIS, AVEVA

## Features

- **Real-Time Logging:** Integrates with Serilog to capture and display log events instantly within the application's UI.
- **Custom ListBox Sink:** Logs are formatted and pushed to a `ListBox`, providing a clear and scrollable log view.
- **Structured Log Entries:** Each log entry includes timestamp, log level, and message for easy analysis.
- **Thread-Safe UI Updates:** Ensures log entries are added to the UI from any thread without cross-thread exceptions.

## Getting Started

### Prerequisites

- [.NET 6.0 SDK or later](https://dotnet.microsoft.com/download)
- Visual Studio 2022 or later (recommended)
- [Serilog](https://serilog.net/) NuGet packages

### Installation

1. Clone the repository:
   ```sh
   git clone https://github.com/yourusername/AISPubSub.git
   ```
2. Open the solution in Visual Studio.
3. Restore NuGet packages.

## Documentation

For detailed information on integration and SDK usage, please refer to the official AVEVA Integration Service SDK documentation:
[AVEVA Integration Service SDK Documentation](https://docs.aveva.com/bundle/integration-service/page/1537081.html)

## Contributing

Contributions are welcome! Please open issues or submit pull requests for enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [Serilog](https://serilog.net/) for structured logging.
- .NET Community for ongoing support and libraries.

---

_For questions or support, please contact the project maintainer._
