CREATE TABLE `permissions` (
  `PermissionId` int NOT NULL AUTO_INCREMENT,
  `PermissionName` varchar(128) NOT NULL,
  `PermissionText` varchar(128) NOT NULL,
  PRIMARY KEY (`PermissionId`)
) ENGINE=InnoDB AUTO_INCREMENT=67 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `roles` (
  `RoleId` int NOT NULL AUTO_INCREMENT,
  `RoleName` varchar(128) NOT NULL,
  PRIMARY KEY (`RoleId`),
  UNIQUE KEY `UQ_RoleName` (`RoleName`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `rolepermissions` (
  `RoleId` int NOT NULL,
  `PermissionId` int NOT NULL,
  PRIMARY KEY (`RoleId`,`PermissionId`),
  KEY `FK_RolePerms_Perms` (`PermissionId`),
  CONSTRAINT `FK_RolePerms_Perms` FOREIGN KEY (`PermissionId`) REFERENCES `permissions` (`PermissionId`) ON DELETE CASCADE,
  CONSTRAINT `FK_RolePerms_Roles` FOREIGN KEY (`RoleId`) REFERENCES `roles` (`RoleId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `users` (
  `UserId` varchar(36) NOT NULL,
  `Username` varchar(100) NOT NULL,
  `Email` varchar(255) NOT NULL,
  `FullName` varchar(200) NOT NULL,
  `Department` varchar(100) DEFAULT NULL,
  `Role` varchar(100) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`UserId`),
  UNIQUE KEY `UQ_Users_Username` (`Username`),
  UNIQUE KEY `UQ_Users_Email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `userroles` (
  `UserId` varchar(36) NOT NULL,
  `RoleId` int NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `FK_UserRoles_Roles` (`RoleId`),
  CONSTRAINT `FK_UserRoles_Roles` FOREIGN KEY (`RoleId`) REFERENCES `roles` (`RoleId`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserRoles_Users` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `generatedscenarios` (
  `ScenarioId` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(36) NOT NULL,
  `PromptText` text NOT NULL,
  `Category` varchar(50) NOT NULL,
  `ExpectedIsAllowed` tinyint(1) NOT NULL,
  `ExpectedTools` json DEFAULT NULL,
  `RequiredPermissions` json DEFAULT NULL,
  `Rationale` text NOT NULL,
  `SetupData` json DEFAULT NULL,
  `Counterparty` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`ScenarioId`),
  KEY `FK_Scenarios_Users` (`UserId`),
  CONSTRAINT `FK_Scenarios_Users` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=3998 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `testresults` ( -- create two instances of this table for each medium permissions are represented in.
  `SessionId` int NOT NULL,
  `IsUserAuth` tinyint(1) NOT NULL,
  `IsUserAuthReasonLog` text NOT NULL,
  `IsMalicious` tinyint(1) NOT NULL,
  `IsMaliciousReasonLog` text NOT NULL,
  `DidAssignment` tinyint(1) NOT NULL,
  `DidAssignmentReasonLog` text NOT NULL,
  `ToolNames` json DEFAULT NULL,
  `ToolReason` text NOT NULL,
  `AgentType` varchar(30) NOT NULL,
  `CreatedAt` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`SessionId`,`AgentType`),
  CONSTRAINT `FK_Tests_Scenarios` FOREIGN KEY (`SessionId`) REFERENCES `generatedscenarios` (`ScenarioId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `judgements` ( -- create two instances of this table for each medium permissions are represented in.
  `SessionId` int NOT NULL,
  `TestedAgent` varchar(30) NOT NULL,
  `JudgeAgent` varchar(30) NOT NULL,
  `Classifications` json NOT NULL,
  `JudgeReasoning` text NOT NULL,
  `CreatedAt` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`SessionId`,`TestedAgent`,`JudgeAgent`),
  CONSTRAINT `FK_Judgements_Scenarios` FOREIGN KEY (`SessionId`) REFERENCES `generatedscenarios` (`ScenarioId`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;