// Fast alle zu tunenden Werte der Freizeit stehen in diesem Script,
// zum Beispiel welche Stimmungsauswirkungen bestimmte Ereignisse haben.

if {[info exists sttsection_tocall]} {
switch $sttsection_tocall {
	"Zwerg" {
		// Störungen
		set stt_dst_pilz						-0.01
		set stt_dst_eatplace					0.05
		set stt_wait_forseat					-0.005
		set stt_dst_bett						-0.02
		set stt_dst_work						-0.005
		set stt_dst_idle						0.0
		set stt_dst_spare						-0.01
		set stt_dst_sex							-0.05
		set stt_dst_talk						-0.02
		// Schlafgewinne, 2-Sekunden-Abstand
		set stt_slpgain_0						0.004
		set stt_slpgain_Zelt					0.006
		set stt_slpgain_Mittelalterschlafzimmer	0.008
		set stt_slpgain_Industrieschlafzimmer	0.009
		set stt_slpgain_Luxusschlafzimmer		0.010
		// Startzivilisationsstufen
		set stt_slpciv_0						-0.1
		set stt_slpciv_Zelt						0.0
		set stt_slpciv_Mittelalterschlafzimmer	0.20
		set stt_slpciv_Industrieschlafzimmer	0.35
		set stt_slpciv_Luxusschlafzimmer		0.50
		set stt_eatciv_0						-0.1
		set stt_eatciv_Feuerstelle				0.10
		set stt_eatciv_Mittelalterkueche		0.30
		set stt_eatciv_Industriekueche			0.40
		set stt_eatciv_Luxuskueche				0.50
		set stt_bthciv_0						-0.1
		set stt_bthciv_Mittelalterbad			0.25
		set stt_bthciv_Industriebad				0.40
		set stt_bthciv_Luxusbad					0.55
		set stt_homciv_0						0.0
		set stt_homeciv_Mittelalterwohnzimmer	0.25
		set stt_homeciv_Industriewohnzimmer		0.40
		set stt_homeciv_Luxuswohnzimmer			0.55
		// Essgewinne
		set stt_eatgain_Grillpilz				0.19
		set stt_eatgain_Grillhamster			0.24
		set stt_eatgain_Raupensuppe				0.31
		set stt_eatgain_Pilzsuppe				0.15 ;# is raus
		set stt_eatgain_Raupenschleimkuchen		0.35
		set stt_eatgain_Goulaschsuppe			0.28 ;# is raus
		set stt_eatgain_Pilzbrot				0.14
		set stt_eatgain_Hamstershake			0.27
		set stt_eatgain_Gourmetsuppe			0.70
		set stt_eatgain_Bier					0.02
		// Geschmack							beefy	sweet	fluid	light
		set stt_eattaste_Grillpilz				{0.01	0.05	0.00	0.05} ;#0.11
		set stt_eattaste_Grillhamster			{0.10	0.00	0.02	0.00} ;#0.12
		set stt_eattaste_Raupensuppe			{0.03	0.02	0.10	0.02} ;#0.17
		set stt_eattaste_Pilzsuppe				{0.00	0.03	0.12	0.07} ;#0.22
		set stt_eattaste_Raupenschleimkuchen	{0.05	0.10	0.03	0.05} ;#0.21
		set stt_eattaste_Goulaschsuppe			{0.12	0.01	0.08	0.00} ;#0.21
		set stt_eattaste_Pilzbrot				{0.00	0.12	0.00	0.06} ;#0.18
		set stt_eattaste_Hamstershake			{0.08	0.04	0.05	0.05} ;#0.22
		set stt_eattaste_Gourmetsuppe			{0.05	0.03	0.07	0.05} ;#0.20
		// Fungewinne
		set stt_fungain_Grillpilz				0.01
		set stt_fungain_Grillhamster			0.03
		set stt_fungain_Pilzbrot				0.02
		set stt_fungain_Raupensuppe				0.00
		set stt_fungain_Raupenschleimkuchen		0.01
		set stt_fungain_Hamstershake			0.15
		set stt_fungain_Gourmetsuppe			0.12
		//
		set stt_maxsearch_range					160.0 ;#darf nicht null sein!
		set stt_disapp_factor					0.3
		set stt_disapp_max						0.2
		// Fun-Aktivitäten
		// Vorzüge
		set stt_exp_Nahrung						{dsc pub}
		set stt_exp_Holz						{pub bwl}
		set stt_exp_Stein						{pub fit}
		set stt_exp_Metall						{bwl dsc}
		set stt_exp_Transport					{dsc tht}
		set stt_exp_Energie						{tht pub}
		set stt_exp_Service						{tht fit}
		set stt_exp_Kampf						{fit bwl}
		// Gesprächsthemengewichtung
		set stt_talk_issues						{wal asw ocw wtm npw fli ttp tlw ubw uqw wmm wti mnf}
		set stt_talkweight_wal					0.1
		set stt_talkweight_asw					0.05
		set stt_talkweight_ocw					1.5
		set stt_talkweight_wtm					0.02
		set stt_talkweight_npw					0.03
		set stt_talkweight_fli					0.2
		set stt_talkweight_ttp					0.02
		set stt_talkweight_tlw					0.05
		set stt_talkweight_ubw					0.1
		set stt_talkweight_uqw					0.2
		set stt_talkweight_wmm					0.2
		set stt_talkweight_wti					0.2
		set stt_talkweight_mnf					0.1
		// Fun-Absichten
		set stt_fun_idleloss					0.0001
		set stt_issue_relief					2.0
		set stt_issue_reduce					0.7
		set stt_fun_intentions					{str fli smo cmf tll lis dft snf cfc pub tht bth dsc fit bow brl}
	}
	"Bier" {
		set stt_Bier_reaction					{{atr_Hitpoints -0.01} {atr_Mood 0.1} {atr_Nutrition 0.02} {atr_Alertness -0.1}}
	}
	"Powerpotion" {
		set stt_Powerpotion_reaction					{{atr_Mood 0.5} {atr_Nutrition 0.05} {atr_Alertness 0.5}}
	}
	"Pilzschnaps" {
		set stt_Schnaps_reaction				{{atr_Hitpoints -0.05} {atr_Mood 0.4} {atr_Nutrition 0.02} {atr_Alertness -0.3}}
	}
}                                                               
}                                                               
