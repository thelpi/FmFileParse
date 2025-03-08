SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

CREATE DATABASE IF NOT EXISTS `cm_save_explorer` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_bin;
USE `cm_save_explorer`;

DROP TABLE IF EXISTS `clubs`;
CREATE TABLE `clubs` (
  `id` int NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `long_name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `country_id` int DEFAULT NULL,
  `reputation` int NOT NULL,
  `division_id` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

DROP TABLE IF EXISTS `competitions`;
CREATE TABLE `competitions` (
  `id` int NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `long_name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `acronym` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `country_id` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

DROP TABLE IF EXISTS `confederations`;
CREATE TABLE `confederations` (
  `id` int NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `acronym` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

DROP TABLE IF EXISTS `countries`;
CREATE TABLE `countries` (
  `id` int NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_bin NOT NULL,
  `is_eu` tinyint(1) NOT NULL,
  `confederation_id` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

DROP TABLE IF EXISTS `countries_backup`;
CREATE TABLE `countries_backup` (
  `id` int NOT NULL,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `is_eu` tinyint(1) NOT NULL,
  `confederation_id` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

INSERT INTO `countries_backup` (`id`, `name`, `is_eu`, `confederation_id`) VALUES
(0, 'Afghanistan', 0, 4),
(1, 'Albanie', 0, 1),
(2, 'Algérie', 0, 2),
(3, 'Samoa Am.', 0, 6),
(4, 'Andorre', 1, 1),
(5, 'Angola', 0, 2),
(6, 'Anguille', 0, 1),
(7, 'Antigua', 0, 6),
(8, 'Argentine', 0, 5),
(9, 'Arménie', 0, 1),
(10, 'Aruba', 0, 1),
(11, 'Australie', 0, 3),
(12, 'Autriche', 1, 1),
(13, 'Azerbaïdjan', 0, 1),
(14, 'Bahamas', 0, 6),
(15, 'Bahreïn', 0, 4),
(16, 'Bangladesh', 0, 4),
(17, 'Barbades', 0, 6),
(18, 'Biélorussie', 0, 1),
(19, 'Belgique', 1, 1),
(20, 'Bélize', 0, 6),
(21, 'Bénin', 0, 2),
(22, 'Bermudes', 0, 1),
(23, 'Boutan', 0, 4),
(24, 'Bolivie', 0, 5),
(25, 'Bosnie-Herzégovine', 0, 1),
(26, 'Botswana', 0, 2),
(27, 'Brésil', 0, 5),
(28, 'Iles Vierges', 0, 6),
(29, 'Sultanat de Brunei', 0, 4),
(30, 'Bulgarie', 0, 1),
(31, 'Burkina Faso', 0, 2),
(32, 'Burundi', 0, 2),
(33, 'C.A.E.M.', 0, NULL),
(34, 'Cambodge', 0, 4),
(35, 'Cameroun', 0, 2),
(36, 'Canada', 0, 6),
(37, 'Iles du Cap Vert', 0, 2),
(38, 'Iles Cayman', 0, 1),
(39, 'République Centrafricaine', 0, 2),
(40, 'Tchad', 0, 2),
(41, 'Chili', 0, 5),
(42, 'Chine', 0, 4),
(43, 'Colombie', 0, 5),
(44, 'Congo', 0, 2),
(45, 'Iles Cook', 0, 3),
(46, 'Costa Rica', 0, 6),
(47, 'Croatie', 0, 1),
(48, 'Cuba', 0, 6),
(49, 'Chypre', 0, 1),
(50, 'République Tchèque', 0, 1),
(51, 'Tchécoslovaquie', 0, NULL),
(52, 'Danemark', 1, 1),
(53, 'Djibouti', 0, 2),
(54, 'Dominique', 0, 6),
(55, 'Dominican Republic', 0, 6),
(56, 'Allemagne de l\'Est', 0, NULL),
(57, 'Equateur', 0, 5),
(58, 'Egypte', 0, 2),
(59, 'Salvador', 0, 6),
(60, 'Angleterre', 1, 1),
(61, 'Guinée Equatoriale', 0, 2),
(62, 'Erythrée', 0, 2),
(63, 'Estonie', 0, 1),
(64, 'Ethiopie', 0, 2),
(65, 'Macédoine', 0, 1),
(66, 'Iles Féroé', 1, 1),
(67, 'Iles Fidji', 0, 3),
(68, 'Finlande', 1, 1),
(69, 'France', 1, 1),
(70, 'Gabon', 0, 2),
(71, 'Gambie', 0, 2),
(72, 'Géorgie', 0, 1),
(73, 'Allemagne', 1, 1),
(74, 'Ghana', 0, 2),
(75, 'Grèce', 1, 1),
(76, 'Grenade', 0, 6),
(77, 'Guam', 0, 6),
(78, 'Guatemala', 0, 6),
(79, 'Guinée', 0, 2),
(80, 'Guinée-Bissau', 0, 2),
(81, 'Guyane Française', 0, 1),
(82, 'Haïti', 0, 6),
(83, 'Pays-Bas', 1, 1),
(84, 'Honduras', 0, 6),
(85, 'Hong Kong', 0, 4),
(86, 'Hongrie', 0, 1),
(87, 'Islande', 1, 1),
(88, 'Inde', 0, 4),
(89, 'Indonésie', 0, 4),
(90, 'Iran', 0, 4),
(91, 'Irak', 0, 4),
(92, 'Eire', 1, 1),
(93, 'Israël', 0, 1),
(94, 'Italie', 1, 1),
(95, 'Côte d\'Ivoire', 0, 2),
(96, 'Jamaïque', 0, 6),
(97, 'Japon', 0, 4),
(98, 'Jordanie', 0, 4),
(99, 'Kazakhstan', 0, 4),
(100, 'Kenya', 0, 2),
(101, 'Koweit', 0, 4),
(102, 'Kirghizistan', 0, 4),
(103, 'Laos', 0, 4),
(104, 'Lettonie', 0, 1),
(105, 'Liban', 0, 4),
(106, 'Lesotho', 0, 2),
(107, 'Libéria', 0, 2),
(108, 'Libye', 0, 2),
(109, 'Liechtenstein', 1, 1),
(110, 'Lituanie', 0, 1),
(111, 'Luxembourg', 1, 1),
(112, 'Macao', 0, 4),
(113, 'Madagascar', 0, 2),
(114, 'Malawi', 0, 2),
(115, 'Malaisie', 0, 4),
(116, 'Maldives', 0, 4),
(117, 'Mali', 0, 2),
(118, 'Malte', 0, 1),
(119, 'Mauritanie', 0, 2),
(120, 'Ile Maurice', 0, 2),
(121, 'Mexique', 0, 6),
(122, 'Moldavie', 0, 1),
(123, 'Mongolie', 0, 4),
(124, 'Montserrat', 0, 1),
(125, 'Maroc', 0, 2),
(126, 'Mozambique', 0, 2),
(127, 'Birmanie', 0, 4),
(128, 'Irlande du Nord', 1, 1),
(129, 'Namibie', 0, 2),
(130, 'Népal', 0, 4),
(131, 'Antilles Néerlandaises', 0, 1),
(132, 'Nouvelle-Calédonie', 0, 1),
(133, 'Nouvelle-Zélande', 0, 3),
(134, 'Nicaragua', 0, 6),
(135, 'Niger', 0, 2),
(136, 'Nigéria', 0, 2),
(137, 'Corée du Nord', 0, 4),
(138, 'Norvège', 1, 1),
(139, 'Oman', 0, 4),
(140, 'Pakistan', 0, 4),
(141, 'Palestine', 0, 4),
(142, 'Panama', 0, 6),
(143, 'Papouasie-Nouvelle-Guinée', 0, 3),
(144, 'Paraguay', 0, 5),
(145, 'Pays Basque', 0, NULL),
(146, 'Pérou', 0, 5),
(147, 'Philippines', 0, 4),
(148, 'Pologne', 0, 1),
(149, 'Portugal', 1, 1),
(150, 'Porto-Rico', 0, 6),
(151, 'Qatar', 0, 4),
(152, 'Congo-Zaïre', 0, 2),
(153, 'Roumanie', 0, 1),
(154, 'Russie', 0, 1),
(155, 'Rwanda', 0, 2),
(156, 'Samoa Occidentales', 0, 3),
(157, 'San Marin', 1, 1),
(158, 'Sao Thomé & Principe', 0, 2),
(159, 'Arabie Saoudite', 0, 4),
(160, 'Ecosse', 1, 1),
(161, 'Sénégal', 0, 2),
(162, 'Seychelles', 0, 2),
(163, 'Sierra Leone', 0, 2),
(164, 'Singapour', 0, 4),
(165, 'Slovaquie', 0, 1),
(166, 'Slovénie', 0, 1),
(167, 'Iles Salomon', 0, 3),
(168, 'Somalie', 0, 2),
(169, 'Afrique du Sud', 0, 2),
(170, 'Corée du Sud', 0, 4),
(171, 'Espagne', 1, 1),
(172, 'Sri Lanka', 0, 4),
(173, 'Saint-Kitts et Névis', 0, 6),
(174, 'Sainte-Lucie', 0, 6),
(175, 'Saint-Vincent', 0, 6),
(176, 'Soudan', 0, 2),
(177, 'Surinam', 0, 5),
(178, 'Swaziland', 0, 2),
(179, 'Suède', 1, 1),
(180, 'Suisse', 1, 1),
(181, 'Syrie', 0, 4),
(182, 'Tahiti', 0, 1),
(183, 'Taiwan', 0, 4),
(184, 'Tadjikistan', 0, 4),
(185, 'Tanzanie', 0, 2),
(186, 'Thaïlande', 0, 4),
(187, 'Timor', 0, 4),
(188, 'Togo', 0, 2),
(189, 'Iles Tonga', 0, 3),
(190, 'Trinité & Tobago', 0, 6),
(191, 'Tunisie', 0, 2),
(192, 'Turquie', 0, 1),
(193, 'Turkménistan', 0, 4),
(194, 'Turks and Caicos Islands', 0, 1),
(195, 'Emirats Arabes Unis', 0, 4),
(196, 'Etats-Unis', 0, 6),
(197, 'Union Soviétique', 0, NULL),
(198, 'Iles Vierges US', 0, 6),
(199, 'Ouganda', 0, 2),
(200, 'Ukraine', 0, 1),
(201, 'Uruguay', 0, 5),
(202, 'Ouzbékistan', 0, 4),
(203, 'Vanuatu', 0, 3),
(204, 'Vénézuela', 0, 5),
(205, 'Vietnam', 0, 4),
(206, 'Allemagne de l\'Ouest', 0, NULL),
(207, 'Pays de Galles', 1, 1),
(208, 'Yémen', 0, 4),
(209, 'Yougoslavie', 0, 1),
(210, 'Zaïre', 0, NULL),
(211, 'Zambie', 0, 2),
(212, 'Zimbabwe', 0, 2);

DROP TABLE IF EXISTS `players`;
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

DROP TABLE IF EXISTS `unmerged_players`;
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

ALTER TABLE `countries_backup`
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
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
