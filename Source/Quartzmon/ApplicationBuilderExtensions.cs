#if ( NETSTANDARD || NETCOREAPP )

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Reflection;
using Microsoft.AspNetCore.Internal;

namespace Quartzmon
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseQuartzmon( this IApplicationBuilder app, QuartzmonOptions options, Action<Services> configure = null )
        {
            options = options ?? throw new ArgumentNullException( nameof( options ) );

            app.UseFileServer( options );

            var services = Services.Create( options );
            configure?.Invoke( services );

            app.Use( async ( context, next ) =>
             {
                 context.Items[typeof( Services )] = services;
                 await next.Invoke();
             } );

       
#if NETCOREAPP
            app.UseEndpoints( endpoints =>
            {
                endpoints.MapControllerRoute( nameof( Quartzmon ), $"{options.VirtualPathRoot}/{{controller=Scheduler}}/{{action=Index}}" );
            } );
#else
            app.UseMvc( routes =>
            {
                routes.MapRoute(
                    name: nameof( Quartzmon ),
                    template: "{controller=Scheduler}/{action=Index}" );
            } );
#endif
        }

        private static void UseFileServer( this IApplicationBuilder app, QuartzmonOptions options )
        {
            IFileProvider fs;
            if ( string.IsNullOrEmpty( options.ContentRootDirectory ) )
                fs = new ManifestEmbeddedFileProvider( Assembly.GetExecutingAssembly(), "Content" );
            else
                fs = new PhysicalFileProvider( options.ContentRootDirectory );

            string baseUrl = options.VirtualPathRoot;
            if (!baseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }
            var fsOptions = new FileServerOptions()
            {
                RequestPath = new PathString( $"{baseUrl}Content" ),
                EnableDefaultFiles = false,
                EnableDirectoryBrowsing = false,
                FileProvider = fs
            };

            app.UseFileServer( fsOptions );
        }

#if NETCOREAPP
        public static void AddQuartzmon( this IServiceCollection services )
        {
            services.AddControllers()
                .AddApplicationPart(Assembly.GetExecutingAssembly());
        }
#else
        public static void AddQuartzmon( this IServiceCollection services )
        {
            services.AddMvcCore()
                .AddApplicationPart(Assembly.GetExecutingAssembly());
        }
#endif

    }
}

#endif
