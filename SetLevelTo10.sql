-- SQL Script to set all users' level to 10
-- This will update the Nivel column for all users in the AspNetUsers table

UPDATE FilmAholicLocalDB.dbo.AspNetUsers
SET Nivel = 100
WHERE Nivel IS NULL OR Nivel < 999;

-- Optional: Show how many users were updated
SELECT COUNT(*) AS UsersUpdated 
FROM FilmAholicLocalDB.dbo.AspNetUsers 
WHERE Nivel = 100;

-- Optional: Verify the update by showing current levels
SELECT Id, UserName, Email, XP, Nivel 
FROM FilmAholicLocalDB.dbo.AspNetUsers
ORDER BY Nivel DESC;
