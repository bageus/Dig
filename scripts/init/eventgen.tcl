// global event specification generator
// objpar = selected object (only one, called multiple if multi-selection)
// subpar = hovered object, world if none
// pospar = mouse position in world
// txtpar = text parameter
//   txtpar elements: -multiselect  : multiselect key is pressed
//                    -alternate    : alternate action key ist pressed
//                    -placement    : subpar object is in placement mode
//                    -command      : actual command

event_generator {objpar subpar pospar txtpar} {



#	log "event generator: $objpar $subpar $pospar $txtpar"

	if { $objpar == 0 } {
		if { $subpar != 0 } {
			return "select"
		} else {
			return "standard"
		}
	}

	set multisel 0
	if {[lsearch $txtpar "-multiselect"] >= 0} {
		set multisel 1
	}
	set alternate 0
	if {[lsearch $txtpar "-alternate"] >= 0} {
		set alternate 1
	}
	set command 0
	if {[lsearch $txtpar "-command"] >= 0} {
		set command 1
	}

	if { $multisel } {
		return "select"
	}

	set srctype [get_objtype $objpar]
	evtgen_attrib -object $objpar
	set otherenemy 0
	set otherworld 0

	if { $subpar == 0 } {
		set subowner -1
	} else {
		set subowner [get_owner $subpar]
	}

	if { [get_owner $objpar] != $subowner } {
		if { $subowner == -1 } {
			set otherworld 1
		}
		set otherenemy 1
	}

	if { $command } {
		//log "event generator: $objpar $subpar $srctype $pospar $txtpar"
	}
	if { [state_get $objpar] == "trapped" } {return }
	if {$srctype=="gnome"} { return [evtgen_gnome $objpar $subpar $pospar $txtpar $otherenemy $otherworld $alternate]}
	if {$srctype=="baby"}  { return [evtgen_baby  $objpar $subpar $pospar $txtpar $otherenemy $otherworld $alternate]}
	if {$srctype=="monster"} { return [evtgen_monster  $objpar $subpar $pospar $txtpar $otherenemy $otherworld $alternate]}
	if { $srctype=="production"  ||  $srctype=="store" ||  $srctype=="protection" || $srctype=="energy" || $srctype=="elevator" } {
		if {[lsearch $txtpar "-placement"] >= 0} {
			return "placement"
		}
		return [evtgen_prod $objpar $subpar $pospar $otherenemy $alternate]
	}
	if {$srctype=="info"}  {
		evtgen_attrib -cursor "INFOOBJ" -desc "INFOOBJ"
	}
	return "select"
//	set subclass [get_objclass $subpar]
//	set subtype [get_objtype $subpar]
//	log "Evt: $srctype $subclass $subtype"
}

proc evtgen_gnome {objpar subpar pospar txtpar otherenemy otherworld alternate} {
	if { $subpar == 0 } {
		set subclass "CObj_World(Jesus himself)"
		set subtype "none"
	} else {
		set subclass [get_objclass $subpar]
		set subtype [get_objtype $subpar]
	}

	set command 0
	if {[lsearch $txtpar "-command"] >= 0} {
		set command 1
	}
	set placement 0
	if {[lsearch $txtpar "-placement"] >= 0} {
		set placement 1
	}
	set invclick 0
	if {[lsearch $txtpar "-invclick"] >= 0} {
		set invclick 1
		set pospar {0 0 0}
	}

	// eventuell ein benutzbares item?
	if { $alternate } {
		if {$subpar != 0} {
			if {[check_method $subclass use]} {
				evtgen_attrib -evtid evt_task_useitem -subject1 $subpar -pos1 $pospar -cursor [lmsg "use"] -desc "[get_objname $objpar] [lmsg use] [get_objname $subpar]"
				return "use"
			}
		}
	}

	// eventuell ein special item, das an einen anderen Gegenstand "gesnapt" ist? (Bilderrätsel etc.)
	// txtpar:	-pos {%f %f %f} -rot {%f %f %f} -snapobj %d
	if {[lsearch $txtpar "-snapobj"] >= 0} {
		set i [lsearch $txtpar "-snapobj"]
		set subpar2 [lindex $txtpar [expr {$i+1}]]
		if {$subpar2 > 0} {
			evtgen_attrib -evtid evt_task_snapitem -subject1 $subpar -subject2 $subpar2 -pos1 $pospar -cursor "" -desc ""
			return "unpack"
	 	}
	}

	if {[inv_find_obj $objpar $subpar] != -1} {
		set isininv 1
	} else {
		set isininv 0
	}

	if {$subpar == 1} {
		if { $alternate } {
			return [evtgen_walk $pospar]
		} else {
			evtgen_attrib -evtid evt_task_dig -pos1 $pospar -cursor [lmsg "dig"] -desc [lmsg "dig"]
			return "dig"
		}
	}
	if {$subtype=="none"} {
		return [evtgen_walk $pospar]
	}
	if {$subtype=="gnome"} {		// other gnome
		set can_attack [is_attackable $objpar $subpar $alternate]
		if {$can_attack} {
			evtgen_attrib -evtid evt_task_attack -subject1 $subpar -pos1 $pospar -cursor [lmsg "attack"] -desc "[get_objname $objpar] [lmsg attack]" -text1 "userevent"
			return "attack"
		}
		if {$alternate} {
			if {$subclass=="Geisel"} {
				evtgen_attrib -evtid evt_task_kidnap -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg kidnap] [lmsg Geisel]"
				return "use"
			} else {
				evtgen_attrib -evtid evt_task_walk -pos1 $pospar -cursor "" -desc ""
				return "walk"
			}
		} else {
			return "select"
		}
	}
	if { $subtype=="production"  ||  $subtype=="store" ||  $subtype=="protection" || $subtype=="energy" || $subtype=="elevator" } {		// production
		if { !$otherenemy && !$alternate && !$isininv } { return "select" }
		set can_attack [is_attackable $objpar $subpar $alternate]
		if {[get_boxed $subpar]} {
			if {$isininv} {
				if {$command} {
					if {$placement} {
						if {$invclick} {
							evtgen_attrib -evtid evt_task_putdown -subject1 $subpar -pos1 {0 0 0} -cursor "own" -desc "[get_objname $objpar] [lmsg putdown] [lmsg $subclass]"
							return "putdown"
						} else {
							if { [string first "asprod" $txtpar] != -1 && !$alternate } {
								evtgen_attrib -evtid evt_task_buildup -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg buildup] [lmsg $subclass]"
								return "unpack"
							} else {
								evtgen_attrib -evtid evt_task_putdown -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg putdown] [lmsg $subclass]"
								return "putdown"
							}
						}
					} else {
						return "placement"
					}
				} else {
					return "unpack"
				}
			} else {
				if { $alternate || $can_attack!=1 } {
					evtgen_attrib -evtid evt_task_pickup -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg pickup] [lmsg $subclass]"
					return "pickup"
				} else {
					// hier muss eigentlich Kistenangriff hin!
					evtgen_attrib -evtid evt_task_pickup -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg pickup] [lmsg $subclass]"
					return "attack"
				}
			}
		} else {
			if {$can_attack==1 || [string match -nocase "*puppe*" $subclass]} {
				evtgen_attrib -evtid evt_task_prodattack -subject1 $subpar -pos1 $pospar -cursor [lmsg "attack"] -desc "[get_objname $objpar] [lmsg attack]"
				return "attack"
			}
			if {$can_attack==2&&$subtype!="elevator"&&$subtype!="protection"} {
				evtgen_attrib -evtid evt_task_buildingconquer -subject1 $subpar -pos1 $pospar -cursor [lmsg "pack"] -desc "[get_objname $objpar] [lmsg conquer]"
				return "pack"
			}
			if {$otherenemy} {
				evtgen_attrib -evtid evt_task_walk -pos1 $pospar -cursor "world" -desc "[get_objname $objpar] world"
				return "walk"
			}
			if {$alternate&&$subtype!="elevator"} {
				evtgen_attrib -evtid evt_task_workprod_prefer -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg assign] [lmsg $subclass]"
				return "assign"
			}
			if { $command } {
				if { $placement } {
					if { $invclick } {
						evtgen_attrib -evtid evt_task_putdown -subject1 $subpar -pos1 {0 0 0} -cursor "own" -desc "[get_objname $objpar] [lmsg putdown] [lmsg $subclass]"
						return "putdown"
					} else {
						if { [string first "asprod" $txtpar] != -1 && !$alternate } {
							evtgen_attrib -evtid evt_task_buildup -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg buildup] [lmsg $subclass]"
							return "unpack"
						} else {
							evtgen_attrib -evtid evt_task_putdown -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg putdown] [lmsg $subclass]"
							return "putdown"
						}
					}
				} else {
					return "placement"
				}
			} else {
				return "unpack"
			}
			return
		}
	}
	if {$subclass=="Pilz"} {
		if { $alternate } {
			return [evtgen_walk $pospar]
		} else {
			evtgen_attrib -evtid evt_task_harvest -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg harvest] [lmsg $subclass]"
			return "dig"
		}
	}

	if {$subtype=="material"} {
		if {$subclass=="Zipfelmuetze"} {
			set submsgtext [get_objname $subpar]
		} else {
			set submsgtext [lmsg $subclass]
		}

		if {$isininv} {
		 	if {$alternate} {
		 		  if {$command} {
		 		  	log "Das Buch aus der Inventory gleich benutzen"
		 		  	evtgen_attrib -evtid evt_task_open_box -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg open] $submsgtext" ;#[lmsg $subclass]"
		 		  }
		 		  return "use"
		 	} else {
				if {$command} {
					if {$placement} {
						if { $invclick } {
							evtgen_attrib -evtid evt_task_putdown -subject1 $subpar -pos1 {0 0 0} -cursor "own" -desc "[get_objname $objpar] [lmsg putdown] $submsgtext"
						} else {
							evtgen_attrib -evtid evt_task_putdown -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg putdown] $submsgtext"
						}
						return "putdown"
					} else {
						return "placement"
					}
				} else {
					return "putdown"
				}
			}
		}
		if { [string range $subclass 0 5] == "Schatz" } {
			if {$subclass=="Schatzbuch"} {
				set buchname [get_objname $subpar]
				if {$alternate} {
						evtgen_attrib -evtid evt_task_open_box -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg readit] $buchname"   ;#[lmsg $subclass]"
						return "use"
				}
				evtgen_attrib -evtid evt_task_pickup -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg pickup] $buchname"   ;#[lmsg Schatzbuch]"
				return "pickup"
			} else {
				evtgen_attrib -evtid evt_task_open_box -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg open] [lmsg $subclass]"
				return "attack"
			}
		}
		if { $alternate } {
			return [evtgen_walk $pospar]
		}
		if {[string first "brocken" $subclass] != -1} {
			evtgen_attrib -evtid evt_task_mine -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg mine] [lmsg $subclass]"
			return "dig"
		} elseif { "Hamster"==$subclass } {
			evtgen_attrib -evtid evt_task_pickup -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg catch] [lmsg Hamster]"
		} elseif {$subclass=="Spinne_tot"} {
			evtgen_attrib -evtid evt_task_open_box -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg mince] [lmsg Spinne]"
			return "attack"
		} else {
			evtgen_attrib -evtid evt_task_pickup -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg pickup] $submsgtext"
		}
		return "pickup"
	}
	if { $subtype=="tool" || $subtype == "transport" } {

		if {!$alternate||$subclass!="Bombe"||[get_attrib $subpar PilzAge]<0.5} {
			if {$isininv} {
				if {$command} {
					if {$placement} {
						if { $invclick } {
							evtgen_attrib -evtid evt_task_putdown -subject1 $subpar -pos1 {0 0 0} -cursor "own" -desc "[get_objname $objpar] [lmsg putdown] [lmsg $subclass]"
						} else {
							evtgen_attrib -evtid evt_task_putdown -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg putdown] [lmsg $subclass]"
						}
						return "putdown"
					} else {
						return "placement"
					}
				} else {
					return "putdown"
				}
			}
		}

		if { $alternate } {
			if {$subclass=="Bombe"} {
				if {[get_attrib $subpar PilzAge]==1.0} {
					evtgen_attrib -evtid evt_task_bombe -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg defuse] [lmsg Bombe]"
					return "use"
				} elseif {[get_attrib $subpar PilzAge]==0.0} {
					evtgen_attrib -evtid evt_task_bombe -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg enable] [lmsg Bombe]"
					return "use"
				} else {
					return
				}
			} else {
				return [evtgen_walk $pospar]
			}
		}

		if {[string match -nocase {schalter*} $subclass]} {
			evtgen_attrib -evtid evt_task_switch -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg switch] [lmsg Schalter]"
			return "use"
		} else {
			switch $subclass {
				"Laseremitter"				{ set actiontext turn }
				"Brainmaschine_Schalter"	{ set actiontext switch }
				"Tuer_LavaK"				{ set actiontext open }
				"Amboss"					{ set actiontext workat }
				"Fenris_greifen"			{ set actiontext grab }
				"Fenris_Stoepsel"			{ set actiontext pullout }
				"Fenris_Krug"				{ set actiontext poison }
				"Quietschewiggle"			{ set actiontext squeeze }
				"Dimensionstor"				{ set actiontext "walk through" }
				default						{ set actiontext "" }
			}
		}
		if { $actiontext != "" } {
			evtgen_attrib -evtid evt_task_objaction -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg $actiontext] [lmsg $subclass]"
			return "use"
		}

		evtgen_attrib -evtid evt_task_pickup -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg pickup] [lmsg $subclass]"
		return "pickup"

	}
	if {$subtype=="monster"} {
		if { !$otherenemy && !$alternate } { return "select" }
		set can_attack [is_attackable $objpar $subpar $alternate]
		if { $can_attack } {
			evtgen_attrib -evtid evt_task_attack -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg attack] [lmsg $subclass]" -text1 "userevent"
			return "attack"
		} else {
			return [evtgen_walk $pospar]
		}
		return
	}
	if {$subtype=="baby"} {
		if { $alternate } {
			if { $otherenemy } {
				evtgen_attrib -evtid evt_task_convert -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg pickup] [lmsg $subclass]"
				return "pickup"
			} else {
				return "select"
			}
		}
		return [evtgen_walk $pospar]
	}
}

proc evtgen_walk {pospar} {
	evtgen_attrib -evtid evt_task_walk -pos1 $pospar -cursor "" -desc ""
	return "walk"
}

proc evtgen_prod {objpar subpar pospar otherenemy alternate} {
	if { $subpar == 0 } {
		set subclass "CObj_World(Jesus himself)"
		set subtype "none"
	} else {
		set subclass [get_objclass $subpar]
		set subtype [get_objtype $subpar]
	}
	if { !$otherenemy && [lsearch {gnome production elevator energy protection baby store monster} $subtype] != -1 } {
		if { $subtype == "gnome" && [get_boxed $objpar] == 0 && $alternate } {
			evtgen_attrib -evtid evt_task_workprod_prefer -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $subpar] [lmsg assign] [lmsg [get_objclass $objpar]]"
			return "assign"
		} else {
			return "select"
		}
	} else {
		return ""
	}
}

proc evtgen_baby {objpar subpar pospar txtpar otherenemy otherworld alternate} {
	if { $subpar == 0 } {
		set subclass "CObj_World(Jesus himself)"
		set subtype "none"
	} else {
		set subclass [get_objclass $subpar]
		set subtype [get_objtype $subpar]
	}

	if { !$otherenemy && !$alternate && [lsearch {gnome production elevator energy protection baby store monster} $subtype] != -1 } {
		return "select"
	}
	evtgen_attrib -evtid evt_task_walk -pos1 $pospar -cursor [lmsg "walkto"] -desc ""
	return "walk"
}

proc evtgen_monster {objpar subpar pospar txtpar otherenemy otherworld alternate} {
	if { $subpar == 0 } {
		set subclass "CObj_World(Jesus himself)"
		set subtype "none"
	} else {
		set subclass [get_objclass $subpar]
		set subtype [get_objtype $subpar]
	}

	if { $subtype == "none" } {
		evtgen_attrib -evtid evt_task_walk -pos1 $pospar -cursor [lmsg "walkto"] -desc ""
		return
	}

	if { !$otherenemy && !$alternate && [lsearch {gnome production elevator energy protection baby store monster} $subtype] != -1 } {
		return "select"
	}

	if {$subtype=="gnome"} {
		set can_attack [expr {$otherenemy && [is_attackable $objpar $subpar $alternate]}]
		if { $can_attack } {
			evtgen_attrib -evtid evt_task_attack -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg attack] [lmsg $subclass]" -text1 "userevent"
			return "attack"
		} else {
			evtgen_attrib -evtid evt_task_walk -pos1 $pospar -cursor "" -desc ""
			return "walk"
		}
	}
	if {$subtype=="monster"} {
		set can_attack [expr {$otherenemy && [is_attackable $objpar $subpar $alternate]}]
		if { $can_attack } {
			evtgen_attrib -evtid evt_task_attack -subject1 $subpar -pos1 $pospar -cursor "own" -desc "[get_objname $objpar] [lmsg attack] [lmsg $subclass]" -text1 "userevent"
			return "attack"
		} else {
			evtgen_attrib -evtid evt_task_walk -pos1 $pospar -cursor "" -desc ""
			return "walk"
		}
	}
}

proc is_attackable {own obj bShift} {

	set oowner [get_owner $own]
	set eowner [get_owner $obj]

	if {$oowner==$eowner} {
		if {[get_objclass $obj]!="Geisel"} {
			return 0
		}
	}
	set subtype [get_objtype $obj]
	if {$eowner==-1} {
		if {$subtype=="monster"||$subtype=="protection"} {
			return 1
		} else {
			return 0
		}
	} else {
		set dipl [get_diplomacy $oowner $eowner]
	}

	if { $bShift } {
		if {[get_objclass $obj]=="Geisel"} {return 0}
		switch $dipl {
			"friend"	{ return 1 }
			"neutral"	{ return 1 }
			"enemy"		{ return 2 }
		}
	} else {
		if {[get_objclass $obj]=="Geisel"} {return 1}
		switch $dipl {
			"friend"	{ return 0 }
			"neutral"	{ return 0 }
			"enemy"		{ return 1 }
		}
	}
	return 0
}

// log "Event generator loaded."

