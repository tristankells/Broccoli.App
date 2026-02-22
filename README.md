# Broccoli.App

## A Cross-Platform Blazor Hybrid Application for Recipe and Food Management

Broccoli.App is a modern, cross-platform application built with .NET MAUI and Blazor Hybrid technology. It aims to provide a seamless experience for managing recipes, tracking food ingredients, and potentially analyzing nutritional information across various devices (Windows, Android, iOS, Mac Catalyst).

## Features

*   **Cross-Platform Compatibility**: Runs on Windows, Android, iOS, and Mac Catalyst from a single codebase.
*   **Blazor Hybrid UI**: Leverages Blazor for rich, interactive web UI components within a native application shell.
*   **Recipe Management**: Store, organize, and view your favorite recipes.
*   **Ingredient Parsing**: Intelligently parses ingredient strings to extract quantity, unit, and food names.
*   **Food Database Integration**: Matches parsed ingredients against a local or remote food database with fuzzy matching.
*   **Nutritional Calculation**: Calculates nutritional values for recipes based on matched ingredients.
*   **Secure Storage**: Utilizes secure storage for sensitive application data.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

*   [.NET SDK](https://dotnet.microsoft.com/download) (Version 10.0 or newer, as indicated by project files)
*   [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (with .NET MAUI workload installed) or [Visual Studio Code](https://code.visualstudio.com/) with C# and .NET MAUI extensions.
*   Workloads for your target platforms (e.g., Android SDK, Xcode for iOS/Mac Catalyst).

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/Broccoli.App.git
    cd Broccoli.App
    ```

2.  **Restore NuGet packages:**
    ```bash
    dotnet restore
    ```

### Running the Application

You can run the application on various platforms.

#### Windows

Open `Broccoli.App.sln` in Visual Studio 2022 and select the "Windows Machine" target. Then, press F5 or click the "Run" button.

Alternatively, from the command line:
```bash
dotnet build Broccoli.App/Broccoli.App.csproj -t:Run -f net10.0-windows10.0.19041.0
```

#### Android

Ensure you have an Android emulator configured or a physical device connected. In Visual Studio 2022, select your desired Android target and press F5.

Alternatively, from the command line:
```bash
dotnet build Broccoli.App/Broccoli.App.csproj -t:Run -f net10.0-android
```

#### iOS / Mac Catalyst

For iOS, you will need a Mac with Xcode installed. For Mac Catalyst, you can run directly on your Mac. In Visual Studio 2022 (on Windows with a paired Mac, or directly on Mac), select your desired iOS/Mac Catalyst target and press F5.

Alternatively, from the command line:
```bash
# For iOS
dotnet build Broccoli.App/Broccoli.App.csproj -t:Run -f net10.0-ios

# For Mac Catalyst
dotnet build Broccoli.App/Broccoli.App.csproj -t:Run -f net10.0-maccatalyst
```

## Project Structure (High-Level)

*   `Broccoli.App/`: The main .NET MAUI application project, containing platform-specific entry points and shared resources.
*   `Broccoli.App.Shared/`: A .NET Standard library containing shared Blazor components, services, models, and business logic used across all platforms.
*   `Broccoli.App.Tests/`: Unit test project for shared logic.
*   `Broccoli.App.Web/`: (Potentially) A web-specific host for the Blazor components, if a pure web version is also intended.

## Contributing

Contributions are welcome! Please see `CONTRIBUTING.md` (if it exists) for details on how to contribute.

## License

This project is licensed under the [MIT License](LICENSE.md) - see the `LICENSE.md` file for details. (Assuming MIT License and a LICENSE.md file exists or will be created).

## Contact

For questions or support, please open an issue on the GitHub repository.
