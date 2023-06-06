using BoardGames.Server.Data.AleaEvangelii.Firestore;
using BoardGames.Server.Hubs;
using Google.Cloud.Firestore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder( args );

builder.Services
	.AddSignalR()
	.AddJsonProtocol( options =>
	{
		options.PayloadSerializerOptions.Converters.Add( new JsonStringEnumConverter( JsonNamingPolicy.CamelCase ) );
	} );

var corsOrigins = builder.Configuration
			.GetValue<string>( "CorsOrigin" )
			?.Split( ',' )
			?? Array.Empty<string>();

builder.Services.AddCors( o =>
{
	o.AddDefaultPolicy( p =>
	{
		p.WithOrigins( corsOrigins )
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();
	} );
} );

FirestoreDb firestoreDb = FirestoreDb.Create( builder.Configuration["Firestore:ProjectId"],
	new Google.Cloud.Firestore.V1.FirestoreClientBuilder()
	{
		JsonCredentials = builder.Configuration["Firestore:CredentialsJsonString"],
	}.Build() );

builder.Services.AddSingleton( firestoreDb );
builder.Services.AddSingleton<IAsyncAERoomStorage, FirestoreAERoomStorage>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors();

app.MapHub<AleaEvangeliiAsyncHub>( "/realtime/alea-evangelii" );

app.Run();
