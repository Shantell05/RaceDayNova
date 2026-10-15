/*
 RaceDay Nova - Part 2
 The Part 1 script seeds placeholder password hashes (PART1_HASH_00x) so the seeded users cannot log in.
 The API's EF Core seed data uses real PBKDF2 hashes instead. If you want a database created by the Part 1 script
 to work with the API too, run this once. All four demo users then have the password: Password123!
*/
USE RaceDayNovaDB;
GO
UPDATE dbo.Users SET PasswordHash = 'PBKDF2$210000$obLD1OX2BxgpOktcbX6PkA==$CQ48tssx3q13qtRSNTBOWdqkGek70mIQ49XpWpQUUAY=' WHERE Email = 'thandi.maseko@racedaynova.co.za';
UPDATE dbo.Users SET PasswordHash = 'PBKDF2$210000$Dx4tPEtaaXiHlqW0w9Lh8A==$MTlLlKeh57PoNRvk96R4DWpUKI9HkzRGz7Q6OSUEwwY=' WHERE Email = 'johan.botha@racedaynova.co.za';
UPDATE dbo.Users SET PasswordHash = 'PBKDF2$210000$Wmt8jZ4PGis8TV5vcIGSow==$bvuwQQSdoZEuv725nN1zHTF33tfvYQ75gF6Uj82ucvA=' WHERE Email = 'lebo.molefe@racedaynova.co.za';
UPDATE dbo.Users SET PasswordHash = 'PBKDF2$210000$kYJzZFVGNygZAKq7zN3u/w==$ijN54yb+uoqyhuxTlItcQPVIf4scq2j9vhJFIF8nkYQ=' WHERE Email = 'zanele.khumalo@racedaynova.co.za';
GO
SELECT UserId, Email, UserRole, LEFT(PasswordHash, 20) AS HashStart FROM dbo.Users;
GO
