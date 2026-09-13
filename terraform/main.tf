terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "= 5.0.0"
    }
  }
}

provider "aws" {
  region = "us-east-1"
}

data "aws_autoscaling_group" "qiglo" {
  name = "qiglo-event-ticketing-asg"
}

data "aws_lb" "qiglo" {
  name = "qiglo-event-ticketing-alb"
}

data "aws_lb_target_group" "qiglo" {
  name = "qiglo-event-ticketing-tg"
}

resource "aws_cloudwatch_dashboard" "qiglo" {
  dashboard_name = "QIGLO-Event-Ticketing"

  dashboard_body = jsonencode({
    widgets = [
      {
        type   = "metric"
        x      = 0
        y      = 0
        width  = 12
        height = 6

        properties = {
          title  = "ASG Average CPU Utilization"
          view   = "timeSeries"
          region = "us-east-1"
          period = 300
          stat   = "Average"

          metrics = [
            [
              "AWS/EC2",
              "CPUUtilization",
              "AutoScalingGroupName",
              data.aws_autoscaling_group.qiglo.name
            ]
          ]
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 0
        width  = 12
        height = 6

        properties = {
          title  = "ALB Target Health"
          view   = "timeSeries"
          region = "us-east-1"
          period = 60

          metrics = [
            [
              "AWS/ApplicationELB",
              "HealthyHostCount",
              "LoadBalancer",
              data.aws_lb.qiglo.arn_suffix,
              "TargetGroup",
              data.aws_lb_target_group.qiglo.arn_suffix
            ],
            [
              "AWS/ApplicationELB",
              "UnHealthyHostCount",
              "LoadBalancer",
              data.aws_lb.qiglo.arn_suffix,
              "TargetGroup",
              data.aws_lb_target_group.qiglo.arn_suffix
            ]
          ]
        }
      },
      {
        type   = "metric"
        x      = 0
        y      = 6
        width  = 24
        height = 6

        properties = {
          title  = "ALB Request Count"
          view   = "timeSeries"
          region = "us-east-1"
          period = 60
          stat   = "Sum"

          metrics = [
            [
              "AWS/ApplicationELB",
              "RequestCount",
              "LoadBalancer",
              data.aws_lb.qiglo.arn_suffix
            ]
          ]
        }
      }
    ]
  })
}

output "dashboard_name" {
  value = aws_cloudwatch_dashboard.qiglo.dashboard_name
}
