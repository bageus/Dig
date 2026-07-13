
proc MakeTTEntry {file name materials tools places attribs} {
	set category [get_class_category $name]
	set type [get_class_type $name]
	set era [get_class_era $name]
	set flags [get_class_flags $name]

	puts $file "{\n$name $category $type $era $flags\n"
	puts $file "// Materials\n{$materials}"
	puts $file "// Tools\n{$tools}"
	puts $file "// Places\n{$places}"
	puts $file "// Attribs\n{$attribs}"
	puts $file "}\n"
}

proc MakeExpEntry {file attribs atrlst} {
	set bitwert 0
	for {set i 0} {$i<[llength $atrlst]} {incr i} {
		if [string match "*[lindex $atrlst $i]*" $attribs] {
			set bitwert [expr int($bitwert+pow(2,$i))]
		}
	}
	set firstwert $bitwert
	if {2<[llength $attribs]} {
		for {set i 0} {$i<[llength $atrlst]} {incr i} {
			if {int(pow(2,$i))&$firstwert} {
				lappend bitwert [expr int(pow(2,$i))^$firstwert]
			}
		}
	}
	if [string index $bitwert 0] {
		foreach nextwert $bitwert {
			seek $file 0 start
			if {-1==[lsearch [gets $file] $nextwert]} {
				seek $file 0 end
			 puts -nonewline $file "$nextwert "
			}
		}
	}
}

set ttfile [open "data/scripts/gameplay/gen_tt.tcl" w]
set expfile [open "data/scripts/gameplay/gen_exp.lst" w+]
puts -nonewline $expfile "1 2 4 8 16 32 64 128 "
set explst "[get_expattrib 0] atr_Kampf"

# [catch {}] == 0 -> okay
foreach cn [ClassList] {
	#log "Class: $cn\n";
	if { ![regexp {^CObj*|^CTclRoot*} $cn] } {
		# log "Testing Class: $cn\n";			
		set fail [catch {
			set items [call_method_static $cn prod_items]
		}]
		if {$fail==0} {
			# log "Techtree Class: $cn\n";			
			# log "$cn    $items\n";
			foreach item $items {
			#	log "  Item: $item\n";
				if {[string first [get_class_type $cn] "productionstoreenergyprotection"]!=-1&&[string first $cn "Zelt"]==-1} {
					set tttsection_tocall $item
					call scripts/misc/techtreetunes.tcl
					set materials [subst \$tttmaterial_$item]
					set tools [subst \$tttinfluence_$item]
					set attribs [subst \$tttinvent_$item]
					catch {unset tttmaterial_$item tttinvent_$item tttgain_$item tttinfluence_$item}
					catch {unset tttpreinv_$item tttitems_$item}
					catch {unset tttenergycons_$item}
					catch {unset tttenergyrange_$item tttenergyvalue_$item tttenergystore_$item tttenergyminstore_$item tttenergyyield_$item}
					catch {unset tttnumber2produce_$item}
				} else {
					set materials [call_method_static $cn prod_item_materials $item]
					set tools [list]
				#	log "    materials: $materials\n";
					if { [check_method $cn prod_item_attribs] } {
						set attribs [call_method_static $cn prod_item_attribs $item]
					} else {
						set attribs [list]
					}
				}
				
				// materials nochmal sortieren, damit sowas nicht auftritt: "Eisen Kohle Eisen Kohle..."
				
				set matlist $materials
				set materials ""
				set donelist ""
				foreach mattype $matlist {
					if {[lsearch $donelist $mattype] < 0} {
						lappend donelist $mattype
						foreach listitem $matlist {
							if {$listitem == $mattype} {
								lappend materials $listitem
							}
						}
					}
				}
				
				MakeTTEntry $ttfile $item $materials $tools [list $cn] $attribs
				MakeExpEntry $expfile $attribs $explst
				
			}
		}
	}
}

close $ttfile
seek $expfile 0 start
log "[llength [gets $expfile]] Attributkombinationen zum Erfinden nötig!"
close $expfile

unset attribs materials tools fail item cn tttsection_tocall ttfile expfile explst
