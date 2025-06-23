# --- 1) Build & publish ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1.1) Copy tất cả code vào context
COPY . .

# 1.2) Publish trực tiếp project API (bao gồm restore tự động)
RUN dotnet publish \
      UserService.API/UserService.API.csproj \
      -c Release \
      -o /app/publish

# --- 2) Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 2.1) Copy bản build đã publish
COPY --from=build /app/publish .

# 2.2) Môi trường & port (tuỳ bạn)
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# 2.3) Chạy API
ENTRYPOINT ["dotnet", "UserService.API.dll"]
