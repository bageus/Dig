//walk & climb anims müssen definiert sein
set_class_anim standstill		riesenhamster.stand
set_class_anim standanim		riesenhamster.standanim

set_class_anim turnleft 		riesenhamster.drehen_links
set_class_anim turnright		riesenhamster.drehen_rechts
set_class_anim turn180left 		riesenhamster.drehen_links
set_class_anim turn180right		riesenhamster.drehen_rechts
set_class_anim rotateleft 		riesenhamster.drehen_links
set_class_anim rotateright		riesenhamster.drehen_rechts
set_class_anim rotate180left 	riesenhamster.drehen_links
set_class_anim rotate180right	riesenhamster.drehen_rechts

set_class_anim walk_start		riesenhamster.gehen_start
set_class_anim walk_loop		riesenhamster.gehen_loop
set_class_anim walk_end			riesenhamster.gehen_end
set_class_anim run				riesenhamster.hoppeln
set_class_anim runwheel			riesenhamster.laufrad

set_class_anim sleep_start		riesenhamster.schlafen_start
set_class_anim sleep_loop		riesenhamster.schlafen_loop
set_class_anim sleep_end		riesenhamster.schlafen_end
set_class_anim clean			riesenhamster.putzen

set_class_anim hit_light		riesenhamster.get_vorn_leicht
set_class_anim hit_hard			riesenhamster.get_vorn_schwer
set_class_anim kungfustillani	riesenhamster.standanim
set_class_anim attack_hit		riesenhamster.schlagen
set_class_anim die				riesenhamster.sterben


// Walk
set_class_animset 0 {
	{standanim			riesenhamster.standanim				}
	{walk_start			riesenhamster.gehen_start			}
	{walk_loop			riesenhamster.gehen_loop			}
	{walk_stop			riesenhamster.gehen_end				}

	{turn_left_90		riesenhamster.drehen_links			}
	{turn_right_90		riesenhamster.drehen_rechts			}
	{turn_left_180		riesenhamster.drehen_links			}
	{turn_right_180		riesenhamster.drehen_rechts			}

	{climb_standanim	riesenhamster.standanim				}
	{climb_up			riesenhamster.standanim				}
	{climb_down			riesenhamster.standanim				}
	{climb_right		riesenhamster.standanim				}
	{climb_left			riesenhamster.standanim				}

	{ground_to_wall		riesenhamster.standanim				}
	{wall_to_ground		riesenhamster.standanim				}

	{walk_loop_wave		riesenhamster.standanim				}
	{ladder_climb_up  	riesenhamster.standanim				}
	{ladder_climb_down	riesenhamster.standanim				}
	{ground_to_ladder	riesenhamster.standanim				}
	{ladder_to_ground	riesenhamster.standanim				}
}


// Run
set_class_animset 1 {
	{walk_loop			riesenhamster.hoppeln				}
}

call scripts/misc/genattribs.tcl   ;# define attribs for all characters
