# How-to-showcase-live-data-updates-in-the-.NET-MAUI-DataGrid-
This demo shows how to showcase live data updates in the .NET MAUI DataGrid.

## TL;DR
Build a real-time data dashboard in .NET MAUI using Syncfusion SfDataGrid by binding dynamic data sources, implementing ObservableCollection and INotifyPropertyChanged for instant updates, and customizing cell templates for visual cues. Includes converters, XAML customization, and performance tips for a smooth, flicker-free UI across Android, iOS, Windows, and macOS.

---

## Introduction
In today’s fast-paced digital world, real-time data is no longer a luxury—it’s a necessity. Whether you’re building a financial dashboard, an IoT monitoring app, or an e-commerce analytics tool, users expect instant updates without refreshing the screen.

**.NET MAUI DataGrid** offers a powerful way to display and update data dynamically across platforms.

### What You’ll Learn
- How to wire data models to SfDataGrid for live updates
- Small, high-impact corrections to make updates smoother and safer
- Visual cues (colors/arrows) to highlight changes
- Performance and UX tips for production dashboards

---

## Why Live Data Updates Matter
- Keep information fresh for better user trust and engagement
- Support faster decisions with up-to-the-second data
- Eliminate manual refresh actions and reduce cognitive load

---

## Why Choose Syncfusion .NET MAUI DataGrid?
- **Smooth, cell-level updates:** Responds instantly to `INotifyPropertyChanged`
- **Rich UX:** Sorting, selection, responsive columns, and templating
- **Performance-focused:** Virtualization and lightweight rendering
- **Flexible styling:** Template columns, triggers, and styles
- **Enterprise-ready:** Backed by dedicated support and production-grade reliability

---

## Sample Architecture
- **Model:** Implements `INotifyPropertyChanged` for cell-level updates
- **Collection:** `ObservableCollection` for runtime adds/removes
- **ViewModel:** Feeds the grid using timers for periodic updates
- **UI:** `SfDataGrid` with template columns and subtle visual cues

---

## Real-Time Live Data Update Procedure
Here’s how to create a dynamic stock dashboard using Syncfusion .NET MAUI DataGrid with live data updates, custom templates, and converters for a visually rich experience.

### XAML Code Example
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:LiveDataSample"
             xmlns:sfgrid="clr-namespace:Syncfusion.Maui.DataGrid;assembly=Syncfusion.Maui.DataGrid"
             x:Class="LiveDataSample.MainPage">

    <ContentPage.BindingContext>
        <local:StockViewModel/>
    </ContentPage.BindingContext>

    <ContentPage.Resources>
        <ResourceDictionary>
            <local:TextForegroundConverter x:Key="textForegroundConverter" />
            <local:ImageConverter x:Key="imageConverter" />
        </ResourceDictionary>
    </ContentPage.Resources>

    <sfgrid:SfDataGrid x:Name="dataGrid"                   
                       x:DataType="local:StockViewModel"
                       ItemsSource="{Binding Stocks}"
                       ColumnWidthMode="Fill"
                       AutoGenerateColumnsMode="None"
                       HorizontalScrollBarVisibility="Always"
                       VerticalScrollBarVisibility="Always">

        <sfgrid:SfDataGrid.Columns>
            <sfgrid:DataGridTextColumn MappingName="Symbol" HeaderText="Symbol" />

            <sfgrid:DataGridTemplateColumn HeaderText="Stock" MappingName="StockChange">
                <sfgrid:DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                        <Grid HorizontalOptions="Center" VerticalOptions="Center" ColumnSpacing="15">
                            <Grid.RowDefinitions>
                                <RowDefinition Height="*" />
                            </Grid.RowDefinitions>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*" />
                                <ColumnDefinition Width="*" />
                            </Grid.ColumnDefinitions>

                            <Label x:Name="label" Grid.Row="0" Grid.Column="0" FontSize="40" x:DataType="local:Stock"
                                   Text="{Binding StockChange, Converter={StaticResource imageConverter}, ConverterParameter={x:Reference label}}" />

                            <Label x:Name="changeValue" Grid.Row="0" Grid.Column="1" x:DataType="local:Stock"
                                   Text="{Binding StockChange, Converter={StaticResource imageConverter}}"
                                   VerticalTextAlignment="Center" FontSize="14" />
                        </Grid>
                    </DataTemplate>
                </sfgrid:DataGridTemplateColumn.CellTemplate>
            </sfgrid:DataGridTemplateColumn>

            <sfgrid:DataGridTextColumn CellTextAlignment="Center" HeaderTextAlignment="Center" HeaderText="Open" MappingName="Open" />
            <sfgrid:DataGridTextColumn HeaderText="Previous Close" MappingName="PreviousClose" />

            <sfgrid:DataGridTemplateColumn HeaderText="Last Trade" HeaderTextAlignment="Start" CellTextAlignment="Start" MappingName="LastTrade">
                <sfgrid:DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                        <Label x:Name="lasttradeValue" x:DataType="local:Stock" Text="{Binding LastTrade}"
                               TextColor="{Binding LastTrade, Converter={StaticResource textForegroundConverter}}"
                               VerticalTextAlignment="Center" />
                    </DataTemplate>
                </sfgrid:DataGridTemplateColumn.CellTemplate>
            </sfgrid:DataGridTemplateColumn>
        </sfgrid:SfDataGrid.Columns>
    </sfgrid:SfDataGrid>
</ContentPage>
```

---

## Run the App
When you run the app on **Android, iOS, Windows, or macOS**, your DataGrid will breathe with data updates every second.

No manual refreshes, no flickering—just smooth, seamless live updates powered by data binding and property change notifications.

![Live Data Update in DataGrid](LiveDataUpdate.gif)

---
## Conclusion
Thanks for reading! In this blog, we’ve seen how to showcase live data updates in `.NET MAUI DataGrid`[https://www.syncfusion.com/maui-controls/maui-datagrid]. Check out our `Release Notes`[https://www.syncfusion.com/products/release-history] and `What’s New pages`[https://www.syncfusion.com/products/whatsnew] to see the other updates in this release and leave your feedback in the comments section below. 
For current Syncfusion customers, the newest version of Essential Studio is available from the `license and downloads page`[https://www.syncfusion.com/Account/Login?ReturnUrl=%2faccount%2fdownloads]. If you are not yet a customer, you can try our 30-day `free trial`[https://www.syncfusion.com/downloads] to check out these new features. 
For questions, you can contact us through our `support forums`[https://www.syncfusion.com/forums], `feedback portal`[https://www.syncfusion.com/feedback], or `support portal`[https://support.syncfusion.com/]. We are always happy to assist you!

