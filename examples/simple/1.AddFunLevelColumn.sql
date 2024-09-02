-- AddFunLevelColumn AFTER AddFunTable

ALTER TABLE "fun" ADD "level" INTEGER NOT NULL DEFAULT 0;

--down--

ALTER TABLE "fun" DROP "level";
