SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";

CREATE TABLE `clubs` (
  `id` int NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `long_name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `country_id` int DEFAULT NULL,
  `reputation` int NOT NULL,
  `division_id` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE `competitions` (
  `id` int NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `long_name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `acronym` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `country_id` int DEFAULT NULL,
  `reputation` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE `confederations` (
  `id` int NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `acronym` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `continent_name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `strength` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE `countries` (
  `id` int NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `acronym` varchar(3) COLLATE utf8mb4_bin NOT NULL,
  `is_eu` tinyint(1) NOT NULL,
  `confederation_id` int DEFAULT NULL,
  `reputation` int NOT NULL,
  `league_standard` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE `players` (
  `id` int NOT NULL,
  `occurences` int NOT NULL,
  `first_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `last_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `common_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `date_of_birth` date NOT NULL,
  `country_id` int NOT NULL,
  `secondary_country_id` int DEFAULT NULL,
  `caps` int NOT NULL,
  `international_goals` int NOT NULL,
  `right_foot` int NOT NULL,
  `left_foot` int NOT NULL,
  `ability` int NOT NULL,
  `potential_ability` int NOT NULL,
  `home_reputation` int NOT NULL,
  `current_reputation` int NOT NULL,
  `world_reputation` int NOT NULL,
  `club_id` int DEFAULT NULL,
  `value` int NOT NULL,
  `contract_expiration` date DEFAULT NULL,
  `wage` int NOT NULL,
  `transfer_status` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `squad_status` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `manager_job_rel` int NOT NULL,
  `min_fee_rel` int NOT NULL,
  `non_play_rel` int NOT NULL,
  `non_promotion_rel` int NOT NULL,
  `relegation_rel` int NOT NULL,
  `pos_goalkeeper` int NOT NULL,
  `pos_sweeper` int NOT NULL,
  `pos_defender` int NOT NULL,
  `pos_defensive_midfielder` int NOT NULL,
  `pos_midfielder` int NOT NULL,
  `pos_attacking_midfielder` int NOT NULL,
  `pos_forward` int NOT NULL,
  `pos_wingback` int NOT NULL,
  `pos_free_role` int NOT NULL,
  `side_left` int NOT NULL,
  `side_right` int NOT NULL,
  `side_center` int NOT NULL,
  `acceleration` int NOT NULL,
  `adaptability` int NOT NULL,
  `aggression` int NOT NULL,
  `agility` int NOT NULL,
  `ambition` int NOT NULL,
  `anticipation` int NOT NULL,
  `balance` int NOT NULL,
  `bravery` int NOT NULL,
  `consistency` int NOT NULL,
  `corners` int NOT NULL,
  `creativity` int NOT NULL,
  `crossing` int NOT NULL,
  `decisions` int NOT NULL,
  `determination` int NOT NULL,
  `dirtiness` int NOT NULL,
  `dribbling` int NOT NULL,
  `finishing` int NOT NULL,
  `flair` int NOT NULL,
  `handling` int NOT NULL,
  `heading` int NOT NULL,
  `important_matches` int NOT NULL,
  `influence` int NOT NULL,
  `injury_proneness` int NOT NULL,
  `jumping` int NOT NULL,
  `long_shots` int NOT NULL,
  `loyalty` int NOT NULL,
  `marking` int NOT NULL,
  `natural_fitness` int NOT NULL,
  `off_the_ball` int NOT NULL,
  `one_on_ones` int NOT NULL,
  `pace` int NOT NULL,
  `passing` int NOT NULL,
  `penalties` int NOT NULL,
  `positioning` int NOT NULL,
  `pressure` int NOT NULL,
  `professionalism` int NOT NULL,
  `reflexes` int NOT NULL,
  `set_pieces` int NOT NULL,
  `sportsmanship` int NOT NULL,
  `stamina` int NOT NULL,
  `strength` int NOT NULL,
  `tackling` int NOT NULL,
  `teamwork` int NOT NULL,
  `technique` int NOT NULL,
  `temperament` int NOT NULL,
  `throw_ins` int NOT NULL,
  `versatility` int NOT NULL,
  `work_rate` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE `players_merge_statistics` (
  `player_id` int NOT NULL,
  `field` varchar(50) COLLATE utf8mb4_bin NOT NULL,
  `occurences` int NOT NULL,
  `merge_type` set('Average','ModeAboveThreshold','ModeBelowThreshold','NonMergeable') CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE `unmerged_players` (
  `id` int NOT NULL,
  `filename` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `first_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `last_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `common_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `date_of_birth` date NOT NULL,
  `country_id` int NOT NULL,
  `secondary_country_id` int DEFAULT NULL,
  `caps` int NOT NULL,
  `international_goals` int NOT NULL,
  `right_foot` int NOT NULL,
  `left_foot` int NOT NULL,
  `ability` int NOT NULL,
  `potential_ability` int NOT NULL,
  `home_reputation` int NOT NULL,
  `current_reputation` int NOT NULL,
  `world_reputation` int NOT NULL,
  `club_id` int DEFAULT NULL,
  `value` int NOT NULL,
  `contract_expiration` date DEFAULT NULL,
  `wage` int NOT NULL,
  `transfer_status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `squad_status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `manager_job_rel` int NOT NULL,
  `min_fee_rel` int NOT NULL,
  `non_play_rel` int NOT NULL,
  `non_promotion_rel` int NOT NULL,
  `relegation_rel` int NOT NULL,
  `pos_goalkeeper` int NOT NULL,
  `pos_sweeper` int NOT NULL,
  `pos_defender` int NOT NULL,
  `pos_defensive_midfielder` int NOT NULL,
  `pos_midfielder` int NOT NULL,
  `pos_attacking_midfielder` int NOT NULL,
  `pos_forward` int NOT NULL,
  `pos_wingback` int NOT NULL,
  `pos_free_role` int NOT NULL,
  `side_left` int NOT NULL,
  `side_right` int NOT NULL,
  `side_center` int NOT NULL,
  `acceleration` int NOT NULL,
  `adaptability` int NOT NULL,
  `aggression` int NOT NULL,
  `agility` int NOT NULL,
  `ambition` int NOT NULL,
  `anticipation` int NOT NULL,
  `balance` int NOT NULL,
  `bravery` int NOT NULL,
  `consistency` int NOT NULL,
  `corners` int NOT NULL,
  `creativity` int NOT NULL,
  `crossing` int NOT NULL,
  `decisions` int NOT NULL,
  `determination` int NOT NULL,
  `dirtiness` int NOT NULL,
  `dribbling` int NOT NULL,
  `finishing` int NOT NULL,
  `flair` int NOT NULL,
  `handling` int NOT NULL,
  `heading` int NOT NULL,
  `important_matches` int NOT NULL,
  `influence` int NOT NULL,
  `injury_proneness` int NOT NULL,
  `jumping` int NOT NULL,
  `long_shots` int NOT NULL,
  `loyalty` int NOT NULL,
  `marking` int NOT NULL,
  `natural_fitness` int NOT NULL,
  `off_the_ball` int NOT NULL,
  `one_on_ones` int NOT NULL,
  `pace` int NOT NULL,
  `passing` int NOT NULL,
  `penalties` int NOT NULL,
  `positioning` int NOT NULL,
  `pressure` int NOT NULL,
  `professionalism` int NOT NULL,
  `reflexes` int NOT NULL,
  `set_pieces` int NOT NULL,
  `sportsmanship` int NOT NULL,
  `stamina` int NOT NULL,
  `strength` int NOT NULL,
  `tackling` int NOT NULL,
  `teamwork` int NOT NULL,
  `technique` int NOT NULL,
  `temperament` int NOT NULL,
  `throw_ins` int NOT NULL,
  `versatility` int NOT NULL,
  `work_rate` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;


ALTER TABLE `clubs`
  ADD PRIMARY KEY (`id`),
  ADD KEY `country_id` (`country_id`),
  ADD KEY `division_id` (`division_id`);

ALTER TABLE `competitions`
  ADD PRIMARY KEY (`id`),
  ADD KEY `country_id` (`country_id`);

ALTER TABLE `confederations`
  ADD PRIMARY KEY (`id`);

ALTER TABLE `countries`
  ADD PRIMARY KEY (`id`),
  ADD KEY `confederation_id` (`confederation_id`),
  ADD KEY `is_eu` (`is_eu`);

ALTER TABLE `players`
  ADD PRIMARY KEY (`id`),
  ADD KEY `club_id` (`club_id`),
  ADD KEY `country_id` (`country_id`),
  ADD KEY `secondary_country_id` (`secondary_country_id`);

ALTER TABLE `unmerged_players`
  ADD PRIMARY KEY (`id`,`filename`),
  ADD KEY `club_id` (`club_id`),
  ADD KEY `country_id` (`country_id`),
  ADD KEY `secondary_country_id` (`secondary_country_id`),
  ADD KEY `first_name` (`first_name`,`last_name`,`common_name`,`filename`);


ALTER TABLE `clubs`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `competitions`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `confederations`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `countries`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `players`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;


ALTER TABLE `clubs`
  ADD CONSTRAINT `clubs_ibfk_1` FOREIGN KEY (`country_id`) REFERENCES `countries` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  ADD CONSTRAINT `clubs_ibfk_2` FOREIGN KEY (`division_id`) REFERENCES `competitions` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE `competitions`
  ADD CONSTRAINT `competitions_ibfk_1` FOREIGN KEY (`country_id`) REFERENCES `countries` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE `countries`
  ADD CONSTRAINT `countries_ibfk_1` FOREIGN KEY (`confederation_id`) REFERENCES `confederations` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE `players`
  ADD CONSTRAINT `players_ibfk_1` FOREIGN KEY (`club_id`) REFERENCES `clubs` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  ADD CONSTRAINT `players_ibfk_2` FOREIGN KEY (`country_id`) REFERENCES `countries` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  ADD CONSTRAINT `players_ibfk_3` FOREIGN KEY (`secondary_country_id`) REFERENCES `countries` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT;
