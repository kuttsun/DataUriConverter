using System;
using System.Text;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.CommandLineUtils;

namespace HtmlBase64Converter
{
    class Program
    {
        static void Main(string[] args)
        {
            // see http://kagasu.hatenablog.com/entry/2016/12/07/004813
            // required System.Text.Encoding.CodePages
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // required Microsoft.Extensions.DependencyInjection
            IServiceCollection serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            CommandLine(serviceProvider).Execute(args);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            ILoggerFactory loggerFactory = new LoggerFactory()
                .AddConsole() // required Microsoft.Extensions.Logging.Console
                .AddDebug();// required Microsoft.Extensions.Logging.Debug

            services.AddSingleton(loggerFactory);
            services.AddLogging();

            // IConfigurationBuilder で設定を選択
            // IConfigurationBuilder.Build() で設定情報を確定し、IConfigurationRoot を生成する
            IConfigurationRoot configuration = new ConfigurationBuilder()
                // 基準となるパスを設定
                // required Microsoft.Extensions.Configuration.FileExtensions
                .SetBasePath(Directory.GetCurrentDirectory())
                // ここでどの設定元を使うか指定
                // 同じキーが設定されている場合、後にAddしたものが優先される
                //.AddJsonFile("appsettings.json", optional: false)
                //.AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                // ここでは JSON より環境変数を優先している
                //.AddEnvironmentVariables()
                // 上記の設定を実際に適用して構成読み込み用のオブジェクトを得る
                .Build();

            // Logger と同じく DI サービスコンテナに Singleton ライフサイクルにてオブジェクトを登録する
            services.AddSingleton(configuration);

            // オプションパターンを有効にすることで、構成ファイルに記述した階層構造データを POCO オブジェクトに読み込めるようにする
            services.AddOptions();

            // Application を DI サービスコンテナに登録する
            // AddTransient はインジェクション毎にインスタンスが生成される
            services.AddTransient<Converter>();
        }

        static CommandLineApplication CommandLine(IServiceProvider serviceProvider)
        {
            // プログラム引数の解析
            var cla = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                // アプリケーション名（ヘルプの出力で使用される）
                Name = Assembly.GetExecutingAssembly().GetName().Name,
            };

            cla.HelpOption("-?|-h|--help");

            var version = cla.Option("-v|--version", "Show version", CommandOptionType.NoValue);
            var input = cla.Option("-i|--input", "Input file", CommandOptionType.SingleValue);
            var output = cla.Option("-o|--output", "Output file", CommandOptionType.SingleValue);
            var dir = cla.Option("-d|--directory", "Input directory", CommandOptionType.SingleValue);

            // backslide 専用
            cla.Command("backslide", command =>
            {
                command.Description = "for backslide";
                command.HelpOption("-?|-h|--help");
                command.OnExecute(() =>
                {
                    serviceProvider.GetService<Converter>().Start(input.Value(), output.Value(), true);
                    return 0;
                });
            });

            // Default behavior
            cla.OnExecute(() =>
            {
                serviceProvider.GetService<Converter>().Start(input.Value(), output.Value());
                Console.ReadKey();
                return 0;
            });

            return cla;
        }
    }
}
