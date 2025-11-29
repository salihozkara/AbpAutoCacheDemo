abp install-libs

cd src/AutoCacheDemo.DbMigrator && dotnet run && cd -



cd src/AutoCacheDemo.Web && dotnet dev-certs https -v -ep openiddict.pfx -p config.auth_server_default_pass_phrase 


exit 0