# PrAnalyzer Solution

PrAnalyzer is a .NET 9 solution for analyzing pull requests using AI-powered code review classification and sentiment analysis. It leverages Blazor for the web frontend and provides API services for integration with GitHub and HuggingFace.

## Solution Structure

- **PrAnalyzer.Web**: Blazor WebAssembly frontend for interacting with the PR analyzer.
- **PrAnalyzer.ApiService**: ASP.NET Core Web API for handling PR data, classification, and external service integration.
- **PrAnalyzer.ServiceDefaults**: Shared service configuration and defaults.
- **PrAnalyzer.AppHost**: Application host for running the solution.

## Features

- Pull request review analysis and classification
- Sentiment and category classification using HuggingFace API
- GitHub integration for fetching PR data
- Modular, extensible architecture

## Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Node.js (for Blazor WebAssembly development)

### Setup
1. Clone the repository:
   ```sh
   git clone <your-repo-url>
   cd PrAnalyzer
   ```
2. Configure API keys in `appsettings.json` (see `PrAnalyzer.ApiService/appsettings.json`):
   - `HuggingFace:ApiKey`: Your HuggingFace API key
   - `GitHub:Token`: (Optional) GitHub personal access token
3. Build the solution:
   ```sh
   dotnet build
   ```
4. Run the application:
   ```sh
   dotnet run --project PrAnalyzer.AppHost
   ```

### Running the Web Frontend
The Blazor frontend is served as part of the AppHost. Navigate to the provided URL after running the host project.

## Configuration
- API keys and service URLs are managed in `appsettings.json` files for each project.
- Logging and environment settings can be customized as needed.

## Contributing
Contributions are welcome! Please open issues or submit pull requests for improvements.

## License
This project is licensed under the MIT License.
