param([switch]$Check)
$ErrorActionPreference='Stop'
$presetRoot=Split-Path -Parent $PSScriptRoot
$data=Get-Content -LiteralPath (Join-Path $presetRoot 'config\haptic_patterns_v1.6.json') -Raw | ConvertFrom-Json
$mapping=[ordered]@{delivery_success='DeliveryOrdered';delivery_out_of_order='DeliveryOutOfOrder';delivery_rejected='DeliveryFailure';
 combo_x2_upgrade='ComboLevel2';combo_x3_upgrade='ComboLevel3';combo_x4_upgrade='ComboLevel4';order_expired='OrderExpired';
 burn_warning='BurnWarning';food_burned='FoodBurned';fire_start='FireStart';fire_reminder='FireReminder';throw='ThrowSuccess';catch='CatchSuccess'}
if(@($data.events.PSObject.Properties).Count -ne $mapping.Count+1 -or $data.settings.max_total_pattern_ms -ne 950){throw 'Unexpected preset schema/events.'}
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('// Generated from config/haptic_patterns_v1.6.json; run scripts/generate-haptic-preset.ps1.')
$lines.Add('using System.Collections.Generic;')
$lines.Add('namespace OC2Haptic.Core')
$lines.Add('{')
$lines.Add('    internal static class V16Preset')
$lines.Add('    {')
$settingsMap=[ordered]@{NormalBudgetMs='normal_active_budget_ms_per_10s';NegativeBudgetMs='negative_active_budget_ms_per_10s';EmergencyBudgetMs='emergency_active_budget_ms_per_10s';StaleNormalMs='drop_stale_normal_event_after_ms';FireReminderIntervalMs='max_same_fire_reminder_interval_ms'}
foreach($constant in $settingsMap.Keys){
 $number=[int]$data.settings.($settingsMap[$constant])
 if($number -le 0 -or $number -gt 10000 -or ($constant -eq 'FireReminderIntervalMs' -and $number -lt 5000)){throw "Invalid policy setting: $constant"}
 $lines.Add(('        internal const int {0}={1};' -f $constant,$number))
}
$lines.Add('        internal static Dictionary<GameEventType,EventFeedback> Create()')
$lines.Add('        {')
$lines.Add('            return new Dictionary<GameEventType,EventFeedback>')
$lines.Add('            {')
foreach($name in $mapping.Keys){
 $entry=$data.events.$name
 if($null -eq $entry -or $entry.category -notin @('positive','neutral','combo','negative','emergency') -or $entry.priority -lt 0 -or $entry.priority -gt 100 -or $entry.cooldown_ms -lt 0 -or $entry.cooldown_ms -gt 60000){throw "Invalid event metadata: $name"}
 $total=0;$segments=[Collections.Generic.List[string]]::new()
 foreach($step in $entry.steps){
  $l=[float]$step.low;$r=[float]$step.high;$ms=[int]$step.ms
  if([float]::IsNaN($l) -or [float]::IsInfinity($l) -or [float]::IsNaN($r) -or [float]::IsInfinity($r) -or $l -lt 0 -or $l -gt 1 -or $r -lt 0 -or $r -gt 1 -or $ms -lt 1 -or $ms -gt 500){throw "Invalid segment: $name"}
  $total+=$ms
  $segments.Add(('new HapticSegment({0}f,{1}f,{2})' -f $l.ToString('0.###',[Globalization.CultureInfo]::InvariantCulture),$r.ToString('0.###',[Globalization.CultureInfo]::InvariantCulture),$ms))
 }
 $extended=$entry.category -in @('negative','emergency');$maximum=if($extended){950}else{500}
 if($segments.Count -lt 1 -or $segments.Count -gt 64 -or $total -gt $maximum){throw "Invalid duration: $name"}
 $factory=if($extended){'HapticPattern.Extended('}else{'new HapticPattern('}
 $category=[Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase([string]$entry.category)
 $enabled=([bool]$entry.enabled).ToString().ToLowerInvariant()
 $lines.Add(('                {{GameEventType.{0},new EventFeedback({1}{2}),{3},{4},FeedbackCategory.{5},{6})}},' -f $mapping[$name],$factory,($segments -join ','),$entry.priority,$entry.cooldown_ms,$category,$enabled))
}
$lines.Add('            };');$lines.Add('        }');$lines.Add('    }');$lines.Add('}')
$output=($lines -join "`n")+"`n"
$target=Join-Path $presetRoot 'src\OC2Haptic.Plugin\Core\V16Preset.cs'
if($Check){if(!(Test-Path -LiteralPath $target) -or ([IO.File]::ReadAllText($target).Replace("`r`n","`n") -ne $output)){throw 'Generated preset is stale; run scripts/generate-haptic-preset.ps1.'};'v1.6 preset validated.'}
else{[IO.File]::WriteAllText($target,$output,[Text.UTF8Encoding]::new($false));'v1.6 preset generated.'}
