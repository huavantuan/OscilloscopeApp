# OscilloscopeApp

Ứng dụng WPF hiển thị dữ liệu dạng oscilloscope 8 kênh, sử dụng ScottPlot và kiến trúc MVVM.

## Tính năng

- Chọn COM port và baudrate
- Hiển thị dữ liệu 8 kênh với 20k điểm mỗi lần
- Thanh trượt để duyệt dữ liệu lớn (10M điểm)
- Kiến trúc MVVM dễ mở rộng và bảo trì

## Công nghệ

- WPF (.NET 6)
- ScottPlot.WPF
- CommunityToolkit.Mvvm
- System.IO.Ports

## Hướng dẫn chạy

```bash
git clone https://github.com/huavantuan/OscilloscopeApp.git
cd OscilloscopeApp
dotnet build
dotnet run
## build release có runtime
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
## build release không có runtime
dotnet publish -c Release -r win-x64 --self-contained false