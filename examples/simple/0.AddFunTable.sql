-- AddFunTable AFTER NONE

CREATE TABLE fun (
    id SERIAL PRIMARY KEY,
    reason TEXT NOT NULL
);

--down--

DROP TABLE fun;