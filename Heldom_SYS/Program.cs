using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Heldom_SYS.Interface;
using Heldom_SYS.Service;
using Heldom_SYS.Models;
using Microsoft.Data.SqlClient;
using Heldom_SYS.Filter;

var builder = WebApplication.CreateBuilder(args);

// �t�m JSON �ǦC�ƿﶵ�]�䴩����^
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.Encoder = JavaScriptEncoder.Create(
        UnicodeRanges.BasicLatin,
        UnicodeRanges.Cyrillic,
        UnicodeRanges.CjkUnifiedIdeographs);
    options.WriteIndented = true;
});

// �K�[ MVC �A��
builder.Services.AddControllersWithViews();

// �K�[ role �A��
builder.Services.AddSingleton<IUserStoreService, UserStoreService>();

//  �[�J Session �]�w
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // �]�w Session ���Įɶ�
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSession(); // �ҥ� Session

builder.Services.AddScoped<IAccidentService, AccidentService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAttendanceExcelService, AttendanceExcelService>();

// �K�[��������
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/Profile/Login"; // ���T���V Login ����
//        options.AccessDeniedPath = "/Profile/AccessDenied";
//    });


// ��Ʈw�s�u �q appsettings.json ��Ū�� Connection �s���r�� �ثe��3��
string? primaryConnection = builder.Configuration.GetConnectionString("ConstructionDB");
string? secondaryConnection = builder.Configuration.GetConnectionString("ConstructionDB2");
string? thirdConnection = builder.Configuration.GetConnectionString("ConstructionDB3");

// ���ըÿ�ܥi�Ϊ��s�u�r��  
string? finalConnection = TestDatabaseConnection(primaryConnection) ? primaryConnection :
                         TestDatabaseConnection(secondaryConnection) ? secondaryConnection :
                         TestDatabaseConnection(thirdConnection) ? thirdConnection :
                         throw new InvalidOperationException("�s�u����");

// �i�H�N�s���r����U��̿�`�J�e�� (�����ϥθ�Ʈw)
builder.Services.AddScoped<SqlConnection>(provider => new SqlConnection(finalConnection));



// ��Ʈw�s�u FEcore(�֨�)   
builder.Services.AddDbContext<ConstructionDbContext>(
            options => options.UseSqlServer(finalConnection));

// user����
builder.Services.AddControllers(options =>
{
    options.Filters.Add<UserFilter>();
});

var app = builder.Build();

app.UseSession(); // �ϥ� Session

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}




app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();




app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}");

//    pattern: "{controller=Project}/{action=BlueprintsCategories}/{id?}");

//    pattern: "{controller=Home}/{action=Index}/{id?}");

//    pattern: "{controller=Dashboard}/{action=Dashboard}/{id?}");




app.Run();

//�˴��s�u
static bool TestDatabaseConnection(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return false;

    try
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            return true;
        }
    }
    catch
    {
        Console.WriteLine($"��Ʈw�s�u����: {connectionString}");
        return false;
    }
}