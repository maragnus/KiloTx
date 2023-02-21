# KiloTx Web Service

The KiloTx web application is designed to host various tools and content for the amateur radio community. 

## Getting Started

1. Open the KiloTx.sln in Visual Studio or JetBrains Rider. 
2. Obtain a MongoDb database and username for a connection string: `mongodb://username:password@host:port/admin`
3. Set up KiloTx.FeedService/appsettings.json
  a. Using .NET User Secrets is recommended
  b. Environmental variables can be used instead: DB__CONNECTIONSTRING and DB__DATABASE
4. Launch the application

## ARRL Bulletins

This application maintains a repository of ARRL Bulletins and serves them in an RSS syndication feed format.

`/arrl/bulletins/yyyy` returns an RSS feed of the provided year
`/arrl/bulletins/yyyy/mm` returns an RSS feed of the provided month
`/arrl/bulletins/update` checks ARRL for new bulletins

## Author

Created by Joshua Brown KC1KTX
