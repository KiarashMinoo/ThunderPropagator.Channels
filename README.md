# RapidStreamer.Channels# RapidStreamer.Channels



**RapidStreamer** is a cutting-edge software solution designed to redefine real-time data streaming. Our mission is to provide **effortless, blazingly fast, and cloud-native streaming capabilities** for maximum impact. This repository contains production-ready channels, demo implementations, and interactive gaming examples that empower developers to build scalable, high-performance streaming applications with ease.**RapidStreamer** is a cutting-edge software solution designed to redefine real-time data streaming. Our mission is to provide **effortless, blazingly fast, and cloud-native streaming capabilities** for maximum impact. This repository contains production-ready channels, demo implementations, and interactive gaming examples that empower developers to build scalable, high-performance streaming applications with ease.



The library includes **7 production channels**, **3 business demos**, and **2 interactive games**, all supporting **.NET 9** and **.NET 8** across multiple platforms including **ARM64**, **x64**, **x86**, and **AnyCPU**. Packages are available from **GitHub Packages**: `https://nuget.pkg.github.com/KiarashMinoo/index.json`.The library includes **7 production channels**, **3 business demos**, and **2 interactive games**, all supporting **.NET 9** and **.NET 8** across multiple platforms including **ARM64**, **x64**, **x86**, and **AnyCPU**. Packages are available from **GitHub Packages**: `https://nuget.pkg.github.com/KiarashMinoo/index.json`.



## Table of Contents## Table of Contents



- [Overview](#overview)- [Overview](#overview)

- [Documentation](#documentation)- [Documentation](#documentation)

- [Features](#features)- [Features](#features)

- [Package Information](#package-information)- [Package Information](#package-information)

- [Installation](#installation)- [Installation](#installation)

- [Quick Start](#quick-start)- [Quick Start](#quick-start)

- [License](#license)- [License](#license)



## Overview## Overview



RapidStreamer.Channels provides a comprehensive collection of pre-built streaming channels and implementations for the RapidStreamer framework, designed to revolutionize real-time data streaming by providing:RapidStreamer.Channels provides a comprehensive collection of pre-built streaming channels and implementations for the RapidStreamer framework, designed to revolutionize real-time data streaming by providing:



- **Effortless Integration**: Simple and intuitive APIs for seamless integration into your applications- **Effortless Integration**: Simple and intuitive APIs for seamless integration into your applications

- **Blazingly Fast Performance**: Optimized for low-latency, high-throughput streaming- **Blazingly Fast Performance**: Optimized for low-latency, high-throughput streaming

- **Cloud-Native Architecture**: Built for modern cloud environments, enabling scalability and resilience- **Cloud-Native Architecture**: Built for modern cloud environments, enabling scalability and resilience

- **Cross-Platform Support**: Compatible with ARM64, x64, x86, and AnyCPU platforms- **Cross-Platform Support**: Compatible with ARM64, x64, x86, and AnyCPU platforms



Whether you're building real-time analytics, live event processing, IoT data pipelines, or interactive applications, RapidStreamer.Channels empowers you to deliver maximum impact with minimal effort.Whether you're building real-time analytics, live event processing, IoT data pipelines, or interactive applications, RapidStreamer.Channels empowers you to deliver maximum impact with minimal effort.



## Documentation## Documentation



📖 **[Complete Documentation](./docs/README.md)** — Comprehensive API reference, usage examples, and architectural guidance📖 **[Complete Documentation](./docs/README.md)** — Comprehensive API reference, usage examples, and architectural guidance



### Quick Links### Quick Links

- **[Production Channels](./docs/Channels/README.md)** — 7 ready-to-use channels for real-world applications- **[Production Channels](./docs/Channels/README.md)** — 7 ready-to-use channels for real-world applications

- **[Demo Implementations](./docs/Demo/README.md)** — 3 business scenario examples with realistic data- **[Demo Implementations](./docs/Demo/README.md)** — 3 business scenario examples with realistic data

- **[Interactive Games](./docs/Games/README.md)** — 2 multiplayer game implementations with advanced patterns- **[Interactive Games](./docs/Games/README.md)** — 2 multiplayer game implementations with advanced patterns



## Features## Features



### Production-Ready Channels### Production-Ready Channels

- **Communication**: Real-time [Chat](./docs/Channels/Chat/README.md) and [Notifications](./docs/Channels/Notifications/README.md)- **Communication**: Real-time [Chat](./docs/Channels/Chat/README.md) and [Notifications](./docs/Channels/Notifications/README.md)

- **Monitoring**: [Network](./docs/Channels/NetworkMonitoring/README.md), [Resource](./docs/Channels/ResourceMonitoring/README.md), and [Throughput](./docs/Channels/Throughput/README.md) monitoring- **Monitoring**: [Network](./docs/Channels/NetworkMonitoring/README.md), [Resource](./docs/Channels/ResourceMonitoring/README.md), and [Throughput](./docs/Channels/Throughput/README.md) monitoring

- **Time-based**: [Clock](./docs/Channels/Clock/README.md) streaming and [TimeZones](./docs/Channels/TimeZones/README.md) with weather integration- **Time-based**: [Clock](./docs/Channels/Clock/README.md) streaming and [TimeZones](./docs/Channels/TimeZones/README.md) with weather integration



### Demo Implementations  ### Demo Implementations  

- **[Airport](./docs/Demo/Airport/README.md)** — Flight tracking and status management- **[Airport](./docs/Demo/Airport/README.md)** — Flight tracking and status management

- **[Portfolio](./docs/Demo/Portfolio/README.md)** — Financial portfolio management with Bogus data generation- **[Portfolio](./docs/Demo/Portfolio/README.md)** — Financial portfolio management with Bogus data generation

- **[StockListBasic](./docs/Demo/StockListBasic/README.md)** — Simple stock market data streaming- **[StockListBasic](./docs/Demo/StockListBasic/README.md)** — Simple stock market data streaming



### Interactive Gaming### Interactive Gaming

- **[RockPaperScissors](./docs/Games/RockPaperScissors/README.md)** — Classic game with player matching- **[RockPaperScissors](./docs/Games/RockPaperScissors/README.md)** — Classic game with player matching

- **[TicTacToe](./docs/Games/TicTacToe/README.md)** — Advanced session management and concurrent gameplay- **[TicTacToe](./docs/Games/TicTacToe/README.md)** — Advanced session management and concurrent gameplay



### Technical Features### Technical Features

- **Cross-Platform Support**: Works seamlessly on ARM64, x64, x86, and AnyCPU platforms- **Cross-Platform Support**: Works seamlessly on ARM64, x64, x86, and AnyCPU platforms

- **.NET Compatibility**: Fully compatible with .NET 9 and .NET 8- **.NET Compatibility**: Fully compatible with .NET 9 and .NET 8

- **Debug and Release Configurations**: Pre-configured for both debug and release builds- **Debug and Release Configurations**: Pre-configured for both debug and release builds

- **High Performance**: Optimized for low-latency, high-throughput streaming- **High Performance**: Optimized for low-latency, high-throughput streaming

- **Cloud-Native**: Designed for modern cloud environments with built-in scalability and resilience- **Cloud-Native**: Designed for modern cloud environments with built-in scalability and resilience



## Package Information## Package Information



### RapidStreamer Dependencies### RapidStreamer Dependencies



All channels depend on the core RapidStreamer framework:All channels depend on the core RapidStreamer framework:



| Package | Version | Description | Repository || Package | Version | Description | Repository |

|---------|---------|-------------|------------||---------|---------|-------------|------------|

| RapidStreamer | 1.0.166-beta.4 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) || RapidStreamer | 1.0.166-beta.4 | Core streaming framework | [GitHub Packages](https://nuget.pkg.github.com/KiarashMinoo/index.json) |



### Platform-Specific Packages### Platform-Specific Packages

The framework supports multiple platforms through conditional package references:The framework supports multiple platforms through conditional package references:

- **AnyCPU**: `RapidStreamer` / `RapidStreamer.Debug`- **AnyCPU**: `RapidStreamer` / `RapidStreamer.Debug`

- **x64**: `RapidStreamer.x64` / `RapidStreamer.Debug.x64`- **x64**: `RapidStreamer.x64` / `RapidStreamer.Debug.x64`

- **x86**: `RapidStreamer.x86` / `RapidStreamer.Debug.x86`- **x86**: `RapidStreamer.x86` / `RapidStreamer.Debug.x86`

- **ARM64**: `RapidStreamer.ARM64` / `RapidStreamer.Debug.ARM64`- **ARM64**: `RapidStreamer.ARM64` / `RapidStreamer.Debug.ARM64`



## Installation## Installation



### Step 1: Add GitHub Packages NuGet Source### Step 1: Add GitHub Packages NuGet Source



The RapidStreamer packages are hosted on GitHub Packages. Add the source using one of these methods:The RapidStreamer packages are hosted on GitHub Packages. Add the source using one of these methods:



#### Using the Command Line:#### Using the Command Line:

```bash```bash

dotnet nuget add source https://nuget.pkg.github.com/KiarashMinoo/index.json -n "GitHub-KiarashMinoo"dotnet nuget add source https://nuget.pkg.github.com/KiarashMinoo/index.json -n "GitHub-KiarashMinoo"

``````



#### Using Visual Studio:#### Using Visual Studio:

1. Open Visual Studio1. Open Visual Studio

2. Go to **Tools** > **NuGet Package Manager** > **Package Manager Settings**2. Go to **Tools** > **NuGet Package Manager** > **Package Manager Settings**

3. Under **Package Sources**, click the **+** button to add a new source3. Under **Package Sources**, click the **+** button to add a new source

4. Enter the following details:4. Enter the following details:

   - **Name**: `GitHub-KiarashMinoo`   - **Name**: `GitHub-KiarashMinoo`

   - **Source**: `https://nuget.pkg.github.com/KiarashMinoo/index.json`   - **Source**: `https://nuget.pkg.github.com/KiarashMinoo/index.json`

5. Click **Update** and then **OK**5. Click **Update** and then **OK**



#### Using nuget.config:#### Using the Command Line:

Create or update your `nuget.config` file in the project/solution root:Add the NuGet source using the following command:

```xml```bash

<?xml version="1.0" encoding="utf-8"?>dotnet nuget add source --name RapidStreamer --source https://nuget.rapidstreamer.com/v3/index.json

<configuration>```

    <packageSources>

        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />#### Create or Update `nuget.config`

        <add key="github" value="https://nuget.pkg.github.com/KiarashMinoo/index.json" />If you don’t already have a `nuget.config` file in your project or solution directory, create one. If you do, update it to include the custom repository.

    </packageSources>

    <packageSourceMapping>Here’s an example of what the `nuget.config` file should look like:

        <packageSource key="github">```xml

            <package pattern="RapidStreamer.*" /><?xml version="1.0" encoding="utf-8"?>

        </packageSource><configuration>

        <packageSource key="nuget.org">    <packageSources>

            <package pattern="*" />        <!-- Add the official NuGet.org source -->

        </packageSource>        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />

    </packageSourceMapping>        <!-- Add the custom RapidStreamer NuGet repository -->

</configuration>        <add key="RapidStreamer" value="https://nuget.rapidstreamer.com/v3/index.json" />

```    </packageSources>

</configuration>

### Step 2: Install RapidStreamer Core Framework```



First, install the core RapidStreamer framework:Place the `nuget.config` file in the root of your solution or project directory. This ensures that all projects in the solution can access the custom NuGet repository.

```bash

dotnet add package RapidStreamer --version 1.0.166-beta.4### Step 2: Verify the Configuration

```

To verify that the custom repository is correctly configured, you can use the following command in the terminal:

### Step 3: Build and Restore```bash

```bashdotnet nuget list source

dotnet restore```

dotnet build -c ReleaseThis will list all configured NuGet sources. You should see something like this in the output:

``````text

Registered Sources:

## Quick Start  1.  nuget.org [Enabled]

      https://api.nuget.org/v3/index.json

### 1. Basic Clock Channel  2.  RapidStreamer [Enabled]

```csharp      https://nuget.rapidstreamer.com/v3/index.json

using Microsoft.Extensions.DependencyInjection;```

using RapidStreamer.Channels.Clock;

### Step 3: Install the NuGet Packages

var services = new ServiceCollection();You can now install the packages using the following commands:

services.AddClockChannel(config => 

{For `RapidStreamer.Channels.Games.RockPaperScissors`:

    config.IsEnabled = true;```bash

});dotnet add package RapidStreamer.Channels.Games.RockPaperScissors

```

var serviceProvider = services.BuildServiceProvider();

var clockChannel = serviceProvider.GetRequiredService<ClockChannel>();For `RapidStreamer.Channels.Games.TicTacToe`:

```bash

await clockChannel.SubscribeAsync("time-subscriber", message => dotnet add package RapidStreamer.Channels.Games.TicTacToe

{```

    Console.WriteLine($"Current time: {message.DateTime}");

});For `RapidStreamer.Channels.Clock`:

``````bash

dotnet add package RapidStreamer.Channels.Clock

### 2. Real-time Notifications```

```csharp

using RapidStreamer.Channels.Notifications;For `RapidStreamer.Channels.NetworkMonitoring`:

```bash

services.AddNotificationsChannel<MyNotificationConfig>(config => dotnet add package RapidStreamer.Channels.NetworkMonitoring

{```

    config.IsEnabled = true;

});For `RapidStreamer.Channels.Notifications`:

```bash

await notificationsChannel.SubscribeAsync("user123", message => dotnet add package RapidStreamer.Channels.Notifications

{```

    Console.WriteLine($"Notification: {message.Subject} - {message.Body}");

});For `RapidStreamer.Channels.ResourceMonitoring`:

``````bash

dotnet add package RapidStreamer.Channels.ResourceMonitoring

### 3. System Monitoring```

```csharp

using RapidStreamer.Channels.NetworkMonitoring;For `RapidStreamer.Channels.Throughput`:

using RapidStreamer.Channels.ResourceMonitoring;```bash

dotnet add package RapidStreamer.Channels.Throughput

services.AddNetworkMonitoringChannel();```

services.AddResourceMonitoringChannel(config => 

{For `RapidStreamer.Channels.TimeZones`:

    config.FeederConfiguration.MemoryUsedPercentageThreshold = 85;```bash

});dotnet add package RapidStreamer.Channels.TimeZones

```

await networkChannel.SubscribeAsync("monitor", message => 

{Alternatively, you can install the packages via the NuGet Package Manager in Visual Studio.

    Console.WriteLine($"Network usage: {message.BytesReceived} bytes received");

});## License

```This project is licensed under the **MIT License**.



For comprehensive examples and API documentation, see the **[full documentation](./docs/README.md)**.© 2024 RapidStreamer. All rights reserved.

## License
This project is licensed under the **MIT License**.

© 2024 RapidStreamer. All rights reserved.