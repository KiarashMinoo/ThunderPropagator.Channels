# Portfolio Demo

[↑ Back to Demo Projects](../README.md) | [→ All Documentation](/docs/README.md)

## Overview

**Domain**: Finance & Trading | **Complexity**: ★★★★☆ Advanced

The **Portfolio Demo** is an investment portfolio management system with real-time position tracking, profit/loss calculations, market data integration, and risk analytics. This demo demonstrates financial calculations, multi-currency support, and high-frequency data updates.

## Key Features

- **Real-Time Valuation**: Live portfolio value updates
- **Position Tracking**: Holdings across stocks, bonds, crypto, etc.
- **P&L Calculations**: Realized & unrealized gains/losses
- **Market Data Integration**: Real-time pricing from external APIs
- **Multi-Currency Support**: FX conversion, base currency normalization
- **Historical Performance**: Time-series tracking, returns calculation
- **Risk Metrics**: Beta, Sharpe ratio, max drawdown
- **Asset Allocation**: Diversification analysis

## Architecture

### Entities
- **Portfolio**: Id, Name, BaseCurrency, TotalValue, Cash
- **Position**: Id, PortfolioId, Symbol, Quantity, CostBasis, CurrentPrice, MarketValue
- **Asset**: Symbol, Name, AssetType, Exchange, Currency
- **Transaction**: Id, PortfolioId, Type, Symbol, Quantity, Price, Fees, Date
- **PriceHistory**: Symbol, Date, Open, High, Low, Close, Volume

### Pipelines (8+)
- `Portfolios/Create` — Create new portfolio
- `Portfolios/GetSummary` — Portfolio overview with P&L
- `Positions/GetAll` — List all positions
- `Positions/GetDetails` — Single position details
- `Transactions/Buy` — Execute buy transaction
- `Transactions/Sell` — Execute sell transaction
- `Transactions/GetHistory` — Transaction log
- `Analytics/GetPerformance` — Returns, risk metrics

### Feeders
- **MarketDataFeeder**: Real-time price updates for held assets
- **PortfolioValuationFeeder**: Periodic portfolio revaluation
- **FxRateFeeder**: Currency exchange rates

## Financial Calculations

### Unrealized P&L
```csharp
UnrealizedPnL = (CurrentPrice - CostBasis) * Quantity
UnrealizedPnLPercent = (CurrentPrice - CostBasis) / CostBasis * 100
```

### Realized P&L
```csharp
RealizedPnL = (SellPrice - CostBasis) * QuantitySold - Fees
```

### Portfolio Return
```csharp
Return = (CurrentValue - InitialInvestment + Withdrawals - Deposits) / InitialInvestment * 100
```

## Usage Example

```csharp
// Register Portfolio channel
services.AddPortfolioChannel(config =>
{
    config.MarketDataApiKey = "your-api-key";
    config.BaseCurrency = "USD";
});

// Client: Create portfolio
var createRequest = new
{
    RequestKey = "Portfolios/Create",
    Name = "My Investment Portfolio",
    BaseCurrency = "USD",
    InitialCash = 100000
};
var portfolio = await channel.SendRequestAsync(createRequest);

// Client: Subscribe to portfolio updates
var subscription = await channel.SubscribeAsync(new Dictionary<string, object>
{
    ["PortfolioId"] = portfolio.Id
});

subscription.OnMessage(message =>
{
    var update = message as PortfolioUpdateMessage;
    Console.WriteLine($"Portfolio Value: ${update.TotalValue:N2}");
    Console.WriteLine($"Total P&L: ${update.TotalPnL:N2} ({update.TotalPnLPercent:F2}%)");
});

// Client: Execute buy transaction
var buyRequest = new
{
    RequestKey = "Transactions/Buy",
    PortfolioId = portfolio.Id,
    Symbol = "AAPL",
    Quantity = 10,
    LimitPrice = 150.00
};
await channel.SendRequestAsync(buyRequest);
```

## Dependencies

- ThunderPropagator 1.0.1-beta.5
- Market Data API (Alpha Vantage, IEX Cloud, etc.)
- FX Rate API (optional for multi-currency)

## Use Cases

- Trading platforms
- Wealth management dashboards
- Robo-advisor applications
- Investment tracking apps
- Financial analytics tools

## See Also

- [Demo Projects Overview](../README.md)
- [StockListBasic Demo](../StockListBasic/README.md) — Market data streaming
- [Throughput Channel](../../Channels/Throughput/README.md) — Performance monitoring

[↑ Back to top](#portfolio-demo)
