//#t_globals.tcl

//#walk anims:
set_class_anim standstill		troll.stehen_warten_a
set_class_anim turnleft 		troll.drehen_links_fit
set_class_anim rotateleft 		troll.drehen_links_fit
set_class_anim turn180left 		troll.drehen_links_ganz
set_class_anim turnright		troll.drehen_rechts_fit
set_class_anim rotateright		troll.drehen_rechts_fit
set_class_anim turn180right		troll.drehen_rechts_ganz
set_class_anim climbup			troll.klettern_auf_langsam
set_class_anim climbdown		troll.klettern_ab_langsam
set_class_anim climbleft		troll.klettern_links
set_class_anim climbright		troll.klettern_rechts
set_class_anim climbtostand		troll.stehen_zu_klettern
set_class_anim standtoclimb		troll.stehen_zu_klettern
set_class_anim climbstill		troll.stehen_zu_klettern
set_class_anim climbstillani	troll.klettern_warten

set_class_anim walkstart		troll.stehen_gehen_start
set_class_anim walkloop			troll.stehen_gehen_loop
set_class_anim walkstop			troll.stehen_gehen_end
set_class_anim runstart			troll.stehen_laufen_start
set_class_anim runloop			troll.stehen_laufen_loop
set_class_anim runstop			troll.stehen_laufen_end

// Walk
set_class_animset 0 {
	{standard			troll.stehen_warten_a		}
	{walk_start			troll.stehen_gehen_start	}
	{walk_loop			troll.stehen_gehen_loop		}
	{walk_stop			troll.stehen_gehen_end		}

	{turn_left_90		troll.drehen_links_fit		}
	{turn_right_90		troll.drehen_rechts_fit		}
	{turn_left_180		troll.drehen_links_ganz		}
	{turn_right_180		troll.drehen_rechts_ganz	}

	{climb_standard		troll.klettern_warten		}
	{climb_up			troll.klettern_auf_langsam	}
	{climb_down			troll.klettern_ab_langsam	}
	{climb_right		troll.klettern_rechts		}
	{climb_left			troll.klettern_links		}

	{ground_to_wall		troll.stehen_zu_klettern	}
	{wall_to_ground		troll.klettern_zu_stehen	}

	{walk_loop_wave		troll.vorne_oben_get_tot	}
	{ladder_climb_up  	troll.klettern_auf_schnell	}
	{ladder_climb_down	troll.klettern_ab_schnell	}
	{ground_to_ladder	troll.stehen_zu_klettern	}
	{ladder_to_ground	troll.klettern_zu_stehen	}
}

// Run
set_class_animset 1 {
	{walk_start			troll.stehen_laufen_start	}
	{walk_loop			troll.stehen_laufen_loop	}
	{walk_stop			troll.stehen_laufen_end		}
}

// Quietschewigglelauf - walktired
set_class_animset 2 {
	{walk_start			troll.131a_quitschewiggle_start	}
	{walk_loop			troll.131a_quitschewiggle_loop	}
	{walk_stop			troll.131a_quitschewiggle_end	}
}

set_class_anim kungfustillani	troll.stehen_warten_a
set_class_anim swordstillani	troll.schwert_warten_a
set_class_anim twohandstillani	troll.speer_warten_a

set_class_anim sleepa			troll.stehen_schlafen;#troll.liegen_schlafen_a
set_class_anim standup			troll.liegen_zu_stehen
set_class_anim salutewaita		troll.salut_warten_a
set_class_anim salutewaitb		troll.salut_warten_b
set_class_anim salutewaitc		troll.salut_warten_c
set_class_anim alarmstop		troll.stehen_alarm_end
set_class_anim alarmloopa		troll.stehen_alarm_loop_a
set_class_anim alarmloopb		troll.stehen_alarm_loop_b
set_class_anim alarmloopc		troll.stehen_alarm_loop_d
set_class_anim alarmstart		troll.stehen_alarm_start
set_class_anim pickup			troll.stehen_aufsammeln
set_class_anim pressmiddle		troll.stehen_druecken_mitte
set_class_anim pressdown		troll.stehen_druecken_unten
set_class_anim discover			troll.stehen_entdecken
set_class_anim clap				troll.stehen_klatschen
set_class_anim scratch			troll.stehen_kratzen
set_class_anim opendown			troll.stehen_oeffnen_unten
set_class_anim splat			troll.stehen_plattmach_tot
set_class_anim splatgetup		troll.stehen_plattmach_reanim
set_class_anim squeeze			troll.stehen_quetschen
set_class_anim sleepb			troll.stehen_schlafen
set_class_anim scout			troll.stehen_spaehen
set_class_anim jump				troll.stehen_sprung
set_class_anim dancestart		troll.stehen_tanzen_start
set_class_anim danceloop		troll.stehen_tanzen_loop
set_class_anim dancestop		troll.stehen_tanzen_end
set_class_anim smash			troll.stehen_truemmern
set_class_anim lookarounda		troll.stehen_umschauen_a
set_class_anim lookaroundb		troll.stehen_umschauen_b
set_class_anim lookaroundc		troll.stehen_umschauen_c
set_class_anim petrified		troll.stehen_versteinert_tot
set_class_anim petrifiedgetup	troll.stehen_versteinert_reanim
set_class_anim splat			troll.stehen_plattmach_tot
set_class_anim splatgetup		troll.stehen_plattmach_reanim
set_class_anim standanima		troll.stehen_warten_a
set_class_anim standanimb		troll.stehen_warten_b
set_class_anim standanimc		troll.stehen_warten_c
set_class_anim laydown			troll.stehen_zu_liegen
set_class_anim fright			troll.stehen_erschrecken
set_class_anim hit				troll.stehen_watschen
set_class_anim drown			troll.stehen_ersaufen

set_class_anim sit_sleep_getup		troll.sitzen_aufwachen
set_class_anim sit_sleep_doze		troll.sitzen_doesen
set_class_anim sit_sleep_fallasleep	troll.sitzen_einschlafen
set_class_anim sit_eatndrink_wipe	troll.sitzen_abwischen
set_class_anim sit_eatndrink_drink	troll.sitzen_saufen_b
set_class_anim sit_eatndrink_eat	troll.sitzen_fressen
set_class_anim sit_misc_gape		troll.sitzen_gaehnen
set_class_anim sit_misc_headshake	troll.sitzen_kopfschuetteln
set_class_anim sit_gamble_dice		troll.sitzen_wuerfeln
set_class_anim sit_gamble_lose		troll.sitzen_verloren
set_class_anim sit_gamble_win		troll.sitzen_gewinn
set_class_anim sit_idle_a			troll.sitzen_warten_a
set_class_anim sit_standup			troll.sitzen_zu_stehen
set_class_anim sit_sitdown			troll.stehen_zu_sitzen

set_class_anim bed_getup_1			troll.bett_a_zu_stehen
set_class_anim bed_getup_2			troll.bett_b_zu_stehen
set_class_anim bed_getup_3			troll.bett_c_zu_stehen
set_class_anim bed_laydown_1		troll.stehen_zu_bett_a
set_class_anim bed_laydown_2		troll.stehen_zu_bett_b
set_class_anim bed_laydown_3		troll.stehen_zu_bett_c
set_class_anim bed_sleep_1			troll.bett_a_schlafen
set_class_anim bed_sleep_2			troll.bett_b_schlafen
set_class_anim bed_sleep_3			troll.bett_c_schlafen


#set_class_anim ladderclimbup	troll.leiter_auf
#set_class_anim ladderclimbdown	troll.leiter_ab
set_class_anim ladderclimbup	troll.klettern_auf_schnell
set_class_anim ladderclimbdown	troll.klettern_auf_schnell

set_class_anim sit_card_lose	troll.sitzen_kartenverloren
set_class_anim sit_card_win		troll.sitzen_kartengewinn
set_class_anim sit_card_hide	troll.sitzen_kartenraus
set_class_anim sit_card_play	troll.sitzen_kartenausspielen
set_class_anim sit_card_sort	troll.sitzen_kartensortieren
set_class_anim sit_card_mix		troll.sitzen_kartenmischen
set_class_anim sit_card_take	troll.sitzen_kartennehmen
set_class_anim sit_card_idle	troll.sitzen_kartenwarten

set_class_anim gape				troll.stehen_gaehnen
set_class_anim itch				troll.stehen_jucken
set_class_anim salut			troll.stehen_salutieren

set_class_anim burn				troll.stehen_brennen_tot

set_class_anim 	fidle0			troll.stehen_kratzen
set_class_anim 	fidle1			troll.stehen_jubeln
set_class_anim  fidle2  		troll.stehen_faustschlag
set_class_anim 	fidle3			troll.stehen_drohen


// --- SEQUENCES - AND - TRAILER -----------------------------

set_class_anim 131jump				troll.131a_hechten
set_class_anim 131funbath			troll.131a_planschen
set_class_anim 131dive				troll.131a_tauchen
set_class_anim 131castaway			troll.131a_treiben
set_class_anim 131reachloop			troll.131a_reichen_loop
set_class_anim 131reachstart		troll.131a_reichen_start
set_class_anim 131wigglestop		troll.131a_quitschewiggle_end
set_class_anim 131wiggleloop		troll.131a_quitschewiggle_loop
set_class_anim 131wigglestart		troll.131a_quitschewiggle_start
set_class_anim 131standboat			troll.131a_stehen_boot

set_class_anim 144nr1				troll.140a_trollnr1
set_class_anim 144nr2				troll.140a_trollnr2
set_class_anim 144nr3				troll.140a_trollnr3
set_class_anim 144nr4				troll.140a_trollnr4
set_class_anim 144nr5				troll.140a_trollnr5

set_class_anim 300fireend			troll.300a_loeschen_end
set_class_anim 300fireloop			troll.300a_loeschen_loop
set_class_anim 300firestart			troll.300a_loeschen_start
set_class_anim 300giggle			troll.300a_kichern

set_class_anim 36chef				troll.36a_meister

// --- END - OF - SEQUENCES - AND - TRAILER ------------------


def_event evt_troll_die
def_event evt_timer0
def_event evt_scan
def_event evt_task_defend
