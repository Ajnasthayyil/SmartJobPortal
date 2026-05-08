-- Ensure IsApproved exists in Recruiters table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Recruiters') AND name = 'IsApproved')
BEGIN
    ALTER TABLE Recruiters ADD IsApproved BIT NOT NULL DEFAULT 0;
END
GO

-- Ensure Location has enough space
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Recruiters') AND name = 'Location')
BEGIN
    ALTER TABLE Recruiters ALTER COLUMN Location NVARCHAR(255);
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Candidates') AND name = 'Location')
BEGIN
    ALTER TABLE Candidates ALTER COLUMN Location NVARCHAR(255);
END
GO

-- Update all existing recruiters to match user approval status if needed
UPDATE r
SET r.IsApproved = u.IsApproved
FROM Recruiters r
INNER JOIN Users u ON r.UserId = u.UserId;
GO
