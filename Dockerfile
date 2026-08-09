FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ExcelDashboard.csproj .
RUN dotnet restore ExcelDashboard.csproj
COPY . .
RUN dotnet publish ExcelDashboard.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# قاعدة بيانات SQLite الحالية (بالبيانات الفعلية) تُنسَخ داخل صورة الحاوية مباشرة - النسخة
# المنشورة "عرض فقط" بلا زر رفع، فلا حاجة لتخزين دائم؛ كل إعادة تشغيل ترجع لنفس هذه النسخة
COPY MadinahDashboard.db .

# Render يمرّر رقم المنفذ عبر متغير البيئة PORT ديناميكيًا؛ 10000 قيمة احتياطية للتشغيل المحلي بالحاوية
ENV PORT=10000
EXPOSE 10000

ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:$PORT dotnet ExcelDashboard.dll"]
