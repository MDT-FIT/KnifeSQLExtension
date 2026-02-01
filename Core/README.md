FOR ANDRII

The core folder contains the base of the application: db connection and parser.
We have to allow our application to read db (in order to compare data in SqlDiffView feature and create schemas within SqlVisualizer feature)
and update db (for RandomDataGeneration feature)

Your classes (services) will be used by others for developing their features

All models should be moved to Models folder

All Constants should be moved to Constants folder

Core logic should be moved to Services