FROM mcr.microsoft.com/dotnet/core/sdk:3.0

ADD . /app

RUN cd /app; dotnet publish -c Release

ENTRYPOINT ["dotnet", "app/bin/Release/netcoreapp3.0/publish/Server.dll"]
