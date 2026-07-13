def_event evt_wuker_die
def_event evt_task_defend

//walk & climb anims müssen definiert sein
set_class_anim standstill		wuker.standard
set_class_anim turnleft 		wuker.drehen_links_a
set_class_anim rotateleft 		wuker.drehen_links_a
set_class_anim turn180left 		wuker.drehen_ganz
set_class_anim turnright		wuker.drehen_rechts_a
set_class_anim rotateright		wuker.drehen_rechts_a
set_class_anim turn180right		wuker.drehen_ganz
set_class_anim climbup			wuker.kletter_hoch
set_class_anim climbdown		wuker.kletter_runter
set_class_anim climbleft		wuker.kletter_links
set_class_anim climbright		wuker.kletter_rechts
set_class_anim climbtostand		wuker.kletterstand_zu_stand
set_class_anim standtoclimb		wuker.stand_zu_kletterstand
set_class_anim climbstill		wuker.kletterstand
set_class_anim climbstillani	wuker.kletter_standanim

set_class_anim climbhita	wuker.kletter_schlagen_a
set_class_anim climbkicka	wuker.kletter_treten_a





// Walk
set_class_animset 0 {
	{standard			wuker.standard				}
	{walk_start			wuker.gehen_start			}
	{walk_loop			wuker.gehen_loop			}
	{walk_stop			wuker.gehen_end				}

	{turn_left_90		wuker.drehen_links_a		}
	{turn_right_90		wuker.drehen_rechts_a		}
	{turn_left_180		wuker.drehen_ganz			}
	{turn_right_180		wuker.drehen_ganz			}

	{climb_standard		wuker.kletter_standanim		}
	{climb_up			wuker.kletter_hoch			}
	{climb_down			wuker.kletter_runter		}
	{climb_right		wuker.kletter_rechts		}
	{climb_left			wuker.kletter_links			}

	{ground_to_wall		wuker.stand_zu_kletterstand	}
	{wall_to_ground		wuker.kletterstand_zu_stand	}

	{walk_loop_wave		wuker.sterben_a				}
	{ladder_climb_up  	wuker.sterben_a				}
	{ladder_climb_down	wuker.sterben_a				}
	{ground_to_ladder	wuker.sterben_a				}
	{ladder_to_ground	wuker.sterben_a				}
}

// Run
set_class_animset 1 {
	{walk_start			wuker.lauf_a_start			}
	{walk_loop			wuker.lauf_a_loop			}
	{walk_stop			wuker.lauf_a_end			}
}

// Sniff
set_class_animset 2 {
	{walk_start			wuker.schnueffeln_b_start	}
	{walk_loop			wuker.schnueffeln_b_loop	}
	{walk_stop			wuker.schnueffeln_b_end		}
}

set_class_anim kungfustillani	wuker.stand_atmen_a

set_class_anim bitea 			wuker.beissen_a
set_class_anim discoverastop 	wuker.entdecken_a_end
set_class_anim discoveraloop	wuker.entdecken_a_loop
set_class_anim discoverastart	wuker.entdecken_a_start
set_class_anim fleeastop		wuker.fliehen_a_end
set_class_anim fleealoop		wuker.fliehen_a_loop
set_class_anim fleeastart		wuker.fliehen_a_start
set_class_anim walkstop			wuker.gehen_end
set_class_anim walkloop			wuker.gehen_loop
set_class_anim walkstart		wuker.gehen_start
set_class_anim walkastop		wuker.gehen_end
set_class_anim walkaloop		wuker.gehen_loop
set_class_anim walkastart		wuker.gehen_start
set_class_anim jumpa 			wuker.hopser_a
set_class_anim lookleft 		wuker.kopf_drehen_links_a
set_class_anim lookright 		wuker.kopf_drehen_rechts_a
set_class_anim scratcha 		wuker.kratzen_a
set_class_anim scratchb 		wuker.kratzen_b
set_class_anim scratchcend 		wuker.kratzen_c_end
set_class_anim scratchcloop 	wuker.kratzen_c_loop
set_class_anim scratchcstart 	wuker.kratzen_c_start
set_class_anim runastop			wuker.lauf_a_end
set_class_anim runaloop 		wuker.lauf_a_loop
set_class_anim runastart 		wuker.lauf_a_start
set_class_anim jumpaftera 		wuker.nachspringen_a
set_class_anim jumpafterb 		wuker.nachspringen_b

set_class_anim sniffstop 		wuker.schnueffeln_b_end
set_class_anim sniffloop 		wuker.schnueffeln_b_loop
set_class_anim sniffstart 		wuker.schnueffeln_b_start
set_class_anim sniffexplorea 	wuker.schnueffeln_entdecken_a
set_class_anim sniffturnleft	wuker.schnueffeln_linksrum
set_class_anim sniffturnright	wuker.schnueffeln_rechtsrum


set_class_anim shakea 			wuker.schuetteln_a
set_class_anim breathea 		wuker.stand_atmen_a
set_class_anim diea 			wuker.sterben_a
set_class_anim drown			wuker.ertrinken
set_class_anim dieminea 		wuker.sterben_mine_a
set_class_anim rampageend 		wuker.trampeln_b_end
set_class_anim rampageloop 		wuker.trampeln_b_loop
set_class_anim rampagestart 	wuker.trampeln_b_start
set_class_anim petrified 		wuker.versteinert_a
set_class_anim splattrap		wuker.hinten_get_schwer_b

set_class_anim falldown			wuker.fallen_loop
set_class_anim falldownhit		wuker.fallen_end_aufstehen
set_class_anim falldowndead		wuker.fallen_end_tot

call scripts/misc/genattribs.tcl   ;# define attribs for all characters
