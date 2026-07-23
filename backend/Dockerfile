FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/KudosQuest.Application/KudosQuest.Application.csproj src/KudosQuest.Application/
COPY src/KudosQuest.API/KudosQuest.API.csproj src/KudosQuest.API/
RUN dotnet restore src/KudosQuest.API/KudosQuest.API.csproj
COPY src/ src/
RUN dotnet publish src/KudosQuest.API/KudosQuest.API.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "KudosQuest.API.dll"]