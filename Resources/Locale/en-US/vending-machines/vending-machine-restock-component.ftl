# SPDX-FileCopyrightText: 2023 Vordenburg <114301317+Vordenburg@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
#
# SPDX-License-Identifier: MIT

vending-machine-restock-invalid-inventory = { CAPITALIZE(THE($this)) } isn't the right package to restock { THE($target) }.
vending-machine-restock-needs-panel-open = { CAPITALIZE(THE($target)) } needs { POSS-ADJ($target) } maintenance panel opened first.
vending-machine-restock-start-self = You start restocking { THE($target) }.
vending-machine-restock-start-others = { CAPITALIZE(THE($user)) } starts restocking { THE($target) }.
vending-machine-restock-done-self = You finish restocking { THE($target) }.
vending-machine-restock-done-others = { CAPITALIZE(THE($user)) } finishes restocking { THE($target) }.
