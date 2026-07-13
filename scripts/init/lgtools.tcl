proc LG_set_templategroup {name code} {
	global tgroups
	set tgroups [list]

	proc tgrouprec {name code} {
		global tgroups
		set pool [list]
		set nochild 0
		if { [string range $name 0 0] == "<" } {
			set nochild 1
		}
		set name [string trim $name "<>"]
		set clength [llength $code]
		for {set i 0} {$i < $clength} {incr i} {
			set element [lindex $code $i]
			set nextelement [lindex $code [expr $i + 1]]
			set fchar [string range $element 0 0]
			if { [string is upper $fchar] } {
				incr i
				set subpool [tgrouprec "$name.$element" $nextelement]
				if { $nochild == 0 } {
					foreach item $subpool {
						lappend pool $item
					}
				}
			} elseif { $fchar == "'" } {
				set pattern [string trim $element "'"]
				set directory [string map "§ $pattern" "data/templates/§.tcl"]
				set files {}
				catch {	set files [glob $directory] }
				foreach file $files {
					set idx [string last "/" $file]
					set file [string range $file [expr $idx + 1] end]
					lappend pool $file
				}
			} else {
				lappend pool $element
			}
		}
		lappend tgroups "$name \{$pool\}"
		return $pool
	}

	proc tgtempfilter { temps } {
		set newtemps [list]
		foreach item $temps {
			if { [string first "." $item] == -1 } {
				set item "$item\.tcl"
			}
			lappend newtemps $item
		}
		return $newtemps
	}


	tgrouprec $name $code


	foreach item $tgroups {
		set name [lindex $item 0]
		set temps [tgtempfilter [lindex $item 1]]
		if { [llength $temps] > 0 } {
			#log "TemplateGroup: $name \{$temps\}"
			lg_set_templategroup $name $temps
			if { [string first ".hol" [string tolower $name]] != -1 } {
				#log "*lg_set_templategroupprops $name -levelcount 1"
				lg_set_templategroupprops $name -levelcount 1 ;#-gamecount 1
			} elseif { [string first ".gsg" [string tolower $name]] != -1 } {
				lg_set_templategroupprops $name -levelcount 1 ;#-gamecount 1
			}
		}
	}
}

proc LG_set_templateprops {props} {
	foreach item $props {
		set name   [lindex $item 0]
		set gcount [lindex $item 1]
		set lcount [lindex $item 2]
		set tunnel [lindex $item 3]
		set gc ""
		if { [string is integer $gcount] } {
			set gc "-gamecount $gcount"
		}
		set lc ""
		if { [string is integer $lcount] } {
			set lc "-levelcount $lcount"
		}
		set tu ""
		if { [string is double $tunnel] } {
			set tu "-tunnel $tunnel"
		}
		if { [string first "." $name] == -1 } {
			set name "$name\.tcl"
		}
		set estr "lg_set_templateprops $name $gc $lc $tu"
		#log $estr
		eval $estr
	}
}

proc get_temp_size {name} {
	if { [string first "." $name] == -1 } {
		set name "$name\.tcl"
	}
	return [lg_get_temp_size $name]
}

proc SM_def_temp_group {argl} {
	for {set i 0} {$i < [llength $argl]} {incr i} {
		set name  [lindex $argl $i]
		incr i
		set tlist [lindex $argl $i]
		sm_def_temp_group $name $tlist
	}
}


