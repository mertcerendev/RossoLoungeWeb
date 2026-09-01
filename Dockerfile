# Yerel test içindir. Windows Smart App Control yerelde derlenen imzasız
# ikilileri engellediği için uygulama Linux konteynerinde çalıştırılıyor.
# Yayına alma bununla yapılmıyor; MonsterASP'e publish çıktısı gönderiliyor.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS derleme
WORKDIR /src

COPY RossoLoungeWeb/RossoLoungeWeb.csproj RossoLoungeWeb/
RUN dotnet restore RossoLoungeWeb/RossoLoungeWeb.csproj

COPY . .
RUN dotnet publish RossoLoungeWeb/RossoLoungeWeb.csproj -c Release -o /uygulama /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=derleme /uygulama .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RossoLoungeWeb.dll"]
